namespace SubscriptionCleanupUtils.Models
{
    public class BackgroundServiceRunningState
    {
        private Dictionary<string, bool> BackgroundServiceRunningStates = new Dictionary<string, bool>();

        public BackgroundServiceRunningState()
        {

        }

        public void RegisterBackgroundService<T>()
            where T: BackgroundService
        {
            Type x = typeof(T);
            this.BackgroundServiceRunningStates.Add(x.Name, true);
        }

        public async Task StopBackgroundService<T>(
            T backgroundService, 
            CancellationToken stoppingToken,
            IHostApplicationLifetime applicationLifetime)
            where T : BackgroundService
        {
            Type x = typeof(T);
            if(this.BackgroundServiceRunningStates.ContainsKey(x.Name))
            {
                this.BackgroundServiceRunningStates[x.Name] = false;
                await backgroundService.StopAsync(stoppingToken);

                List<bool> states = this.BackgroundServiceRunningStates.Values.ToList();
                List<bool> running = states.Where(x => x ==  true).ToList();
                if(running.Count == 0)
                {
                    applicationLifetime.StopApplication();
                }

            }
        }
    }
}
