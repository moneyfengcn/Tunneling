

using Tunneling.Core;

namespace Tunneling.Server.Models.MapProxy
{
    public class MapGroup
    {
        required public string AccessToken { get; set; }
        required public string GroupName { get; set; }
        required public List<MapProxyModel> MapProxy { get; set; }
    }
    public class MapProxyModel
    {
        required public string Name { get; set; }
        required public int PublicPort { get; set; }
        required public string LocalHost { get; set; }
        required public int LocalPort { get; set; }

        public BanPolicy? Policy { get; set; }=null;        
    }
}
