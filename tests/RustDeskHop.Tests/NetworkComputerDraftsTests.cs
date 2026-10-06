using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class NetworkComputerDraftsTests
    {
        #region Test Methods
        [Fact]
        public void AddRenameAndRemoveAreAtomicAndScopedToOneNetwork()
        {
            TargetDefinition first = Computer("private", "111");
            TargetDefinition second = Computer("private", "222");
            TargetDefinition other = Computer("public", "111");
            List<TargetDefinition> saved = [first, other, second];
            NetworkComputerDrafts drafts = new NetworkComputerDrafts("private", saved);
            drafts.Items[0].Name = "  Workstation  ";
            drafts.Items.RemoveAt(1);
            Assert.True(drafts.TryAdd(Computer("private", "333"), out string? error));
            Assert.Null(error);
            Assert.Equal(3, saved.Count);
            Assert.Equal("Computer 111", first.Name);
            Assert.Contains(second, saved);
            Assert.True(drafts.TrySave(saved, out error));
            Assert.Null(error);
            Assert.Equal(["111", "111", "333"], saved.Select(t => t.RustDeskId));
            Assert.Equal("Workstation", first.Name);
            Assert.Same(other, saved[1]);
            Assert.Equal("Computer 111", other.Name);
            Assert.Equal("private", saved[2].ProfileId);
            Assert.True(drafts.TrySave(saved, out _));
            Assert.Equal(3, saved.Count);
        }

        [Fact]
        public void InvalidNameDoesNotPartiallyApplyAddsRemovalsOrRenames()
        {
            List<TargetDefinition> saved = [Computer("private", "111"), Computer("private", "222")];
            NetworkComputerDrafts drafts = new NetworkComputerDrafts("private", saved);
            drafts.Items.RemoveAt(0);
            drafts.Items[0].Name = " ";
            Assert.True(drafts.TryAdd(Computer("private", "333"), out _));
            Assert.False(drafts.TrySave(saved, out string? error));
            Assert.Contains("needs a name", error);
            Assert.Equal(["111", "222"], saved.Select(t => t.RustDeskId));
            Assert.Equal("Computer 222", saved[1].Name);
        }

        [Fact]
        public void ReloadDiscardsDraftAddsRemovalsAndLabels()
        {
            List<TargetDefinition> saved = [Computer("private", "111"), Computer("private", "222")];
            NetworkComputerDrafts drafts = new NetworkComputerDrafts("private", saved);
            drafts.Items[0].Name = "Not saved";
            drafts.Items.RemoveAt(1);
            Assert.True(drafts.TryAdd(Computer("private", "333"), out _));
            NetworkComputerDrafts reloaded = new NetworkComputerDrafts("private", saved);
            Assert.Equal(["111", "222"], reloaded.Items.Select(d => d.RustDeskId));
            Assert.Equal("Computer 111", reloaded.Items[0].Name);
        }

        [Fact]
        public void DuplicateIdsAreRejectedOnlyWithinTheSelectedNetwork()
        {
            List<TargetDefinition> saved = [Computer("public", "111")];
            NetworkComputerDrafts drafts = new NetworkComputerDrafts("private", saved);
            Assert.True(drafts.TryAdd(Computer("private", "111"), out _));
            Assert.False(drafts.TryAdd(Computer("private", " 111 "), out string? duplicate));
            Assert.Contains("already saved", duplicate);
            Assert.False(drafts.TryAdd(Computer("public", "222"), out _));
            Assert.False(drafts.TryAdd(Computer("private", " "), out _));
            Assert.Single(drafts.Items);
        }

        [Fact]
        public void RemovingEveryComputerDoesNotRemoveOtherNetworksOrProfiles()
        {
            TargetDefinition other = Computer("public", "222");
            List<TargetDefinition> saved = [Computer("private", "111"), other];
            NetworkComputerDrafts drafts = new NetworkComputerDrafts("private", saved);
            drafts.Items.Clear();
            Assert.True(drafts.TrySave(saved, out _));
            Assert.Same(other, Assert.Single(saved));
        }
        #endregion

        #region Methods
        private static TargetDefinition Computer(string profileId, string id) => new TargetDefinition
        {
            Name = $"Computer {id}", ProfileId = profileId, RustDeskId = id
        };
        #endregion
    }
}