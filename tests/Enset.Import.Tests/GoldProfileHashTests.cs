using Enset.Application.GoldProfiles;using Xunit;
namespace Enset.Import.Tests;
public sealed class GoldProfileHashTests{
 [Fact]public void Hash_is_deterministic(){Assert.Equal(GoldProfileHash.Create(new{Usage="School",Area=100m}).Hash,GoldProfileHash.Create(new{Usage="School",Area=100m}).Hash);}
 [Fact]public void Relevant_change_changes_hash(){Assert.NotEqual(GoldProfileHash.Create(new{Usage="School",Area=100m}).Hash,GoldProfileHash.Create(new{Usage="School",Area=101m}).Hash);}
}
