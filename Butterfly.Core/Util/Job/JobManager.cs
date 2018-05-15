using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NLog;

namespace Butterfly.Util.Job {
    public class JobManager {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly List<JobData> jobs = new List<JobData>();

        protected readonly float firstRunDelaySeconds;
        protected readonly float noJobsDelaySeconds;

        public JobManager(float firstRunDelaySeconds = 15.0f, float noJobsDelaySeconds = 0.5f) {
            this.firstRunDelaySeconds = firstRunDelaySeconds;
            this.noJobsDelaySeconds = noJobsDelaySeconds;
        }

        public void AddJob(IJob job, DateTime nextRunAt) {
            this.jobs.Add(new JobData(job, nextRunAt));
        }

        protected CancellationTokenSource cancellationTokenSource;
        protected bool stopped = false;
        public void Start() {
            this.cancellationTokenSource = new CancellationTokenSource();

            try {
                this.stopped = false;
                Task.Run(() => Run(), this.cancellationTokenSource.Token);
            }
            catch (TaskCanceledException e) {
                logger.Debug(e);
            }
        }

        public void Stop() {
            if (!this.stopped) {
                this.cancellationTokenSource.Cancel();
            }
            this.stopped = true;
        }

        protected async Task Run() {
            await Task.Delay((int)Math.Round(this.firstRunDelaySeconds * 1000), this.cancellationTokenSource.Token);

            while (!this.stopped) {
                if (this.jobs.Count == 0) {
                    await Task.Delay((int)Math.Round(this.noJobsDelaySeconds * 1000), this.cancellationTokenSource.Token);
                }
                else {
                    this.jobs.Sort((job1, job2) => job1.nextRunAt.CompareTo(job2.nextRunAt));

                    TimeSpan timeSpan = this.jobs[0].nextRunAt - DateTime.Now;
                    int delay = (int)timeSpan.TotalMilliseconds;
                    if (delay > 0) {
                        await Task.Delay(delay);
                    }

                    DateTime? nextRunAt = await this.jobs[0].job.Run();
                    if (nextRunAt == null) this.jobs.RemoveAt(0);
                    else this.jobs[0].nextRunAt = nextRunAt.Value;
                }
            }
        }
    }

    public class JobData {
        public readonly IJob job;
        public DateTime nextRunAt;

        public JobData(IJob job, DateTime nextRunAt) {
            this.job = job;
            this.nextRunAt = nextRunAt;
        }
    }
}
