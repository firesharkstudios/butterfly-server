using System;
using System.Threading.Tasks;

namespace Butterfly.Util.Job {
    public interface IJob {
        Task<DateTime?> Run();
    }
}
