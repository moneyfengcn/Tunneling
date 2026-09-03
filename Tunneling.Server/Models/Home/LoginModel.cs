using System.ComponentModel.DataAnnotations;
using Tunneling.Server;
 


namespace Tunneling.Server.Models.Home
{
    public class LoginModel
    {

        [Display(Name = "用户名")]
        [StringLength(20)]
        required public string UserName { get; set; }

        [Display(Name = "密码")]

        [StringLength(20)]
        [DataType(DataType.Password)]

        required public string Password { get; set; }


        [Display(Name = "记住我")]
        public bool RememberMe { get; set; } = true;
    }
}
