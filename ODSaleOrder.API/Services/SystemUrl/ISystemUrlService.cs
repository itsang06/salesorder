using SysAdmin.Models.SystemUrl;
using System.Threading.Tasks;

namespace SysAdmin.Web.Services.SystemUrl
{
    public interface ISystemUrlService
    {
        public Task<SystemUrlListModel> GetAllSystemUrl();
    }
}
