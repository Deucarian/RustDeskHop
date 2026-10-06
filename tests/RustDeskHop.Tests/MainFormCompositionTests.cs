using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class MainFormCompositionTests
    {
        #region Test Methods
        [Theory]
        [InlineData((int)ConnectionOutcome.STARTED)]
        [InlineData((int)ConnectionOutcome.CANCELLED)]
        [InlineData((int)ConnectionOutcome.BLOCKED)]
        [InlineData((int)ConnectionOutcome.FAILED)]
        public void ConnectDelegatesToWorkflowAndRestoresControlsAfterEveryOutcome(int outcome)
        {
            OnSta(() =>
                  {
                      ConnectionTestRig rig = new ConnectionTestRig();
                      TargetDefinition selected = new TargetDefinition
                      {
                          Name = "Test",
                          RustDeskId = "123",
                          ProfileId = rig.Public.Id
                      };
                      rig.Settings.Targets.Add(new TargetDefinition
                                               {
                                                   Name = "Not clicked", RustDeskId = "456", ProfileId = rig.Private.Id
                                               }
                                              );
                      rig.Settings.Targets.Add(selected);
                      PendingWorkflow workflow = new PendingWorkflow();
                      using MainForm form = new MainForm(new MemorySettingsStore(rig.Settings),
                                                         workflow,
                                                         _ => rig.Interaction
                                                        );
                      form.Show();
                      Application.DoEvents();
                      MethodInfo connect =
                          typeof(MainForm).GetMethod("ConnectRowAsync",
                                                     BindingFlags.Instance | BindingFlags.NonPublic
                                                    )!;
                      ComputerGrid grid = (ComputerGrid)Assert.Single(form.Controls.Find("Computers", true));
                      grid.CurrentCell = grid.Rows[0].Cells[0];
                      Task pending = Assert.IsAssignableFrom<Task>(connect.Invoke(form, [1]));
                      Assert.False(pending.IsCompleted);
                      Assert.Same(selected, workflow.Target);
                      Assert.Same(rig.Settings, workflow.Settings);
                      Assert.Same(rig.Interaction, workflow.Interaction);
                      Assert.Equal("Opening…", grid.Rows[1].Cells["Connect"].Value);
                      string[] names = new[]
                      {
                          "ManageNetworks",
                          "Computers"
                      };
                      Assert.All(names, name => Assert.False(Assert.Single(form.Controls.Find(name, true)).Enabled));

                      // A second click/Enter invocation while waiting must not launch twice.
                      Assert.IsAssignableFrom<Task>(connect.Invoke(form, [0])).GetAwaiter().GetResult();
                      Assert.Equal(1, workflow.Calls);
                      workflow.Completion.SetResult((ConnectionOutcome)outcome);
                      DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                      while (!pending.IsCompleted && DateTime.UtcNow < deadline)
                      {
                          Application.DoEvents();
                          Thread.Sleep(1);
                      }

                      Assert.True(pending.IsCompleted, "UI continuation did not finish.");
                      pending.GetAwaiter().GetResult();
                      Assert.All(names, name => Assert.True(Assert.Single(form.Controls.Find(name, true)).Enabled));
                      Assert.All(grid.Rows.Cast<DataGridViewRow>(),
                                 row => Assert.Equal("Connect", row.Cells["Connect"].Value)
                                );
                      Assert.Empty(rig.RustDesk.LaunchedTargets);
                  }
                 );
        }

        [Fact]
        public void RowButtonEnterAndSpaceConnectTheCorrespondingComputer()
        {
            OnSta(() =>
                  {
                      ConnectionTestRig rig = new ConnectionTestRig();
                      rig.Settings.Targets.AddRange([
                                                        new TargetDefinition
                                                        {
                                                            Name = "First", RustDeskId = "111",
                                                            ProfileId = rig.Public.Id
                                                        },
                                                        new TargetDefinition
                                                        {
                                                            Name = "Second", RustDeskId = "222",
                                                            ProfileId = rig.Private.Id
                                                        }
                                                    ]
                                                   );
                      RecordingWorkflow workflow = new RecordingWorkflow();
                      using MainForm form =
                          new MainForm(new MemorySettingsStore(rig.Settings), workflow, _ => rig.Interaction);
                      form.Show();
                      Application.DoEvents();
                      ComputerGrid grid = (ComputerGrid)Assert.Single(form.Controls.Find("Computers", true));
                      grid.CurrentCell = grid.Rows[0].Cells[0];
                      typeof(ComputerGrid).GetMethod("OnCellContentClick",
                                                     BindingFlags.Instance | BindingFlags.NonPublic
                                                    )!
                          .Invoke(grid, [new DataGridViewCellEventArgs(3, 1)]);
                      Assert.Equal(["222"], workflow.Ids);
                      typeof(ComputerGrid).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .Invoke(grid, [new KeyEventArgs(Keys.Enter)]);
                      Assert.Equal(["222", "111"], workflow.Ids);
                      grid.CurrentCell = grid.Rows[1].Cells[3];
                      typeof(ComputerGrid).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .Invoke(grid, [new KeyEventArgs(Keys.Space)]);
                      typeof(DataGridView).GetMethod("OnKeyUp", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .Invoke(grid,
                                  [new KeyEventArgs(Keys.Space)]
                                 );
                      Assert.Equal(["222", "111", "222"], workflow.Ids);
                      Assert.Empty(rig.RustDesk.LaunchedTargets);
                  }
                 );
        }
        #endregion

        #region Methods
        private static void OnSta(Action action)
        {
            Exception? failure = null;
            Thread thread = new Thread(() =>
                                       {
                                           try
                                           {
                                               action();
                                           }
                                           catch (Exception ex)
                                           {
                                               failure = ex;
                                           }
                                       }
                                      );
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "Composed UI test timed out.");
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
        #endregion

        #region Nested Types
        private sealed class RecordingWorkflow : IConnectionWorkflow
        {
            #region Properties and Indexers
            internal List<string> Ids { get; } = [];
            #endregion

            #region Methods
            public Task<ConnectionOutcome> ConnectAsync(TargetDefinition target,
                                                        AppSettings settings,
                                                        IConnectionInteraction interaction)
            {
                Ids.Add(target.RustDeskId);
                return Task.FromResult(ConnectionOutcome.STARTED);
            }
            #endregion
        }

        private sealed class PendingWorkflow : IConnectionWorkflow
        {
            #region Properties and Indexers
            internal TaskCompletionSource<ConnectionOutcome> Completion { get; } =
                new TaskCompletionSource<ConnectionOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);

            internal int Calls { get; private set; }
            internal TargetDefinition? Target { get; private set; }
            internal AppSettings? Settings { get; private set; }
            internal IConnectionInteraction? Interaction { get; private set; }
            #endregion

            #region Methods
            public Task<ConnectionOutcome> ConnectAsync(TargetDefinition target,
                                                        AppSettings settings,
                                                        IConnectionInteraction interaction)
            {
                Calls++;
                Target = target;
                Settings = settings;
                Interaction = interaction;
                return Completion.Task;
            }
            #endregion
        }
        #endregion
    }
}