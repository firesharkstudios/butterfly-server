using System;
using System.Threading.Tasks;

namespace Butterfly.Core.Util.Job {
    public interface IJob {
        Task<DateTime?> Run();
    }
}
