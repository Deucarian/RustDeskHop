using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Fact]
        public void PopulatedComputersTabStartsWithTheListWithoutInstructionText()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      NetworkComputersEditor editor = Find<NetworkComputersEditor>(form, "NetworkComputersEditor");
                      DataGridView grid = Find<DataGridView>(editor, "NetworkComputers");
                      Assert.DoesNotContain(Descendants(editor).OfType<Label>(), label => label.Visible);
                      Assert.Equal(editor.PointToScreen(Point.Empty), grid.PointToScreen(Point.Empty));
                      Assert.Empty(Descendants(editor).OfType<ModernButton>());
                      Assert.IsType<RowActionCell>(grid.Rows[0].Cells["TestComputer"]);
                      Assert.IsType<RowActionCell>(grid.Rows[0].Cells["RemoveComputer"]);
                      Assert.Equal("+ Add computer", grid.Rows[grid.Rows.Count - 1].Cells[0].Value);
                  }
                 );
        }

        [Fact]
        public void SecondaryManagementButtonRendersBlueTextOnWhite()
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      Load(form);
                      ModernButton button = Find<ModernButton>(form, "ManageNetworks");
                      using Bitmap bitmap = new Bitmap(button.Width, button.Height);
                      button.DrawToBitmap(bitmap, button.ClientRectangle);
                      Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(10, button.Height / 2).ToArgb());
                      int bluePixels = 0;
                      for (int y = 0; y < bitmap.Height; y++)
                      {
                          for (int x = 0; x < bitmap.Width; x++)
                          {
                              Color pixel = bitmap.GetPixel(x, y);
                              if (Math.Abs(pixel.R - AppTheme.blue.R) < 10
                                  && Math.Abs(pixel.G - AppTheme.blue.G) < 10
                                  && Math.Abs(pixel.B - AppTheme.blue.B) < 10)
                                  bluePixels++;
                          }
                      }
                      Assert.True(bluePixels > 20, "The management label should visibly use the blue accent.");
                      Assert.True(button.TabStop);
                  }
                 );
        }
        #endregion
    }
}