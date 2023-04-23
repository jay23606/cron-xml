using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Xml.Linq;

namespace cron;
public class CronService : BackgroundService
{
    readonly List<Job> _jobs = new List<Job>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ReadJobs();
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobGroups = _jobs.Where(j => j.IsActive && j.GroupIsActive && j.IsDue()).GroupBy(j => j.Group).ToList();
            var taskList = new List<Task>();
            foreach (var jobGroup in jobGroups)
            {
                taskList.Add(Task.Run(async () =>
                {
                    foreach (var job in jobGroup) foreach (var task in job.Tasks.Where(t => t.IsActive)) await RunTask(job, task);
                }));
            }
            await Task.WhenAll(taskList);
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }

    async Task RunTask(Job job, Tsk task)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = task.FileName,
                Arguments = task.Arguments,
                WorkingDirectory = task.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        var logPath = Path.Combine(Directory.GetCurrentDirectory(), $"{job.Group}-{job.Name}-{task.Name}.log");
        //await using var streamWriter = new StreamWriter(logPath, true);
        var lineCount = 0;
        process.OutputDataReceived += async (sender, args) =>
        {
            if (args.Data == null) return;
            
            await using var streamWriter = new StreamWriter(logPath, true);
            
            DateTime time = DateTime.UtcNow;
            if (job.TimeZone != "")
            {
                TimeZoneInfo mountainZone = TimeZoneInfo.FindSystemTimeZoneById(job.TimeZone!);
                time = TimeZoneInfo.ConvertTimeFromUtc(time, mountainZone);
            }
            var data = $"[{time.ToString("yy-MM-dd HH:mm:ss")}] {args.Data}";
            lineCount++;
            if (lineCount <= task.MaxLogLines)
            {
                streamWriter.WriteLine(data);
                Console.WriteLine(data);
            }
            else
            {
                var lines = File.ReadAllLines(logPath).Skip(task.MaxLogLines - lineCount);
                File.WriteAllLines(logPath, lines);
                streamWriter.WriteLine(data);
                Console.WriteLine(data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync();
    }

    void ReadJobs()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), Environment.GetCommandLineArgs()[1]);
        var document = XDocument.Load(filePath);

        _jobs.Clear();

        int groupNum=1, jobNum = 1, taskNum = 1;
        foreach (var groupElement in document.Root!.Elements("group"))
        {
            foreach (var jobElement in groupElement.Elements("job"))
            {
                var mins = jobElement.Attribute("minutely")?.Value;
                var hours = jobElement.Attribute("hourly")?.Value;
                var days = jobElement.Attribute("daily")?.Value;
                Schedule? schedule = null;
                if (mins != null) schedule = Schedule.Parse(mins);
                else if (hours != null) schedule = Schedule.ParseHours(hours);
                else if (days != null) schedule = Schedule.ParseDays(days);
                else schedule = Schedule.ParseDays("1"); //run once a day by default


                var job = new Job
                {
                    Name = jobElement.Attribute("name")?.Value ?? $"job{jobNum}",
                    IsActive = (jobElement.Attribute("active")?.Value ?? "true").ToLower() == "true",
                    Schedule = schedule,
                    TimeZone = jobElement.Attribute("timeZone")?.Value ?? "",
                    Group = groupElement.Attribute("name")?.Value ?? $"group{groupNum}",
                    GroupIsActive = (groupElement.Attribute("active")?.Value ?? $"true").ToLower() == "true"
                };
                foreach (var taskElement in jobElement.Elements("task"))
                {
                    var task = new Tsk
                    {
                        Name = taskElement.Attribute("name")?.Value ?? $"task{taskNum}",
                        IsActive = (taskElement.Attribute("active")?.Value ?? "true").ToLower() == "true",
                        FileName = taskElement.Element("FileName")?.Value,
                        Arguments = taskElement.Element("Arguments")?.Value ?? "",
                        WorkingDirectory = taskElement.Element("WorkingDirectory")?.Value ?? "",
                        MaxLogLines = Convert.ToInt32(taskElement.Element("MaxLogLines")?.Value ?? "100")
                    };
                    job.Tasks.Add(task);
                    taskNum++;
                }
                _jobs.Add(job);
                jobNum++;
            }
            groupNum++;
        }
    }

    class Job
    {
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public Schedule Schedule { get; set; } = new Schedule();
        public List<Tsk> Tasks { get; } = new List<Tsk>();
        public bool IsDue() => Schedule.IsDue();
        public string? TimeZone { get; set; }
        public string? Group { get; set; }
        public bool GroupIsActive { get; set; }
    }

    class Tsk
    {
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public string? FileName { get; set; }
        public string? Arguments { get; set; }
        public string? WorkingDirectory { get; set; }
        public int MaxLogLines { get; set; }
    }

    class Schedule
    {
        private DateTime _lastRunTime;
        public int Minutes { get; set; }
        public static Schedule Parse(string value) =>
            (int.TryParse(value, out var minutes)) ? new Schedule { Minutes = minutes } : new Schedule();
        public static Schedule ParseHours(string value) =>
            (int.TryParse(value, out var minutes)) ? new Schedule { Minutes = minutes * 60 } : new Schedule();
        public static Schedule ParseDays(string value) =>
            (int.TryParse(value, out var minutes)) ? new Schedule { Minutes = minutes * 60 * 24 } : new Schedule();
        public bool IsDue()
        {
            var now = DateTime.Now;
            if (now - _lastRunTime >= TimeSpan.FromMinutes(Minutes))
            {
                _lastRunTime = now;
                return true;
            }
            return false;
        }
    }
}