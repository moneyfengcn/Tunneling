using System.ComponentModel.DataAnnotations;
using Tunneling.Server.Models.MapProxy;

namespace Tunneling.Server.Infrastructure
{
    public class SystemConfig
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<MapGroup> MapGroups { get; set; }        
    }
}
