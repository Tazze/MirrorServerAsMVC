using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MirrorServerAsMVC.Startup))]
namespace MirrorServerAsMVC
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
