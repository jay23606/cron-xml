# cron-xml
Simple cron-like execution of apps based on XML input

Currently it is set up to run jobs and tasks serially and groups of jobs/tasks in parallel 

Simply pass it the path to your executable as the FileName and by default it was run that executable once a day

Review this sample xml for more options such as Arguments, WorkingDirectory for the task

You can also give each group, job, and task a name and specify: 
minutely=<minutes>
hourly=<hours>
daily=<days>

You can also set whether group/job/task is active or not and the timeZone for the job (default is UTC time)

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
	<group name="group1" active="true">
		<job name="job1" active="true" minutely="2">
			<task name="task1" active="true">
				<FileName>C:\Users\jayab\source\repos\hello-world\bin\Debug\net6.0\hello-world.exe</FileName>
				<Arguments>dir test1</Arguments>
				<WorkingDirectory>C:\Users\jayab</WorkingDirectory>
			</task>
			<task name="task2" active="true">
				<FileName>C:\Users\jayab\source\repos\hello-world\bin\Debug\net6.0\hello-world.exe</FileName>
				<Arguments>dir test1 test2</Arguments>
				<WorkingDirectory>C:\Users\jayab\source\repos\hello-world</WorkingDirectory>
			</task>
			<!--minimum of info needed-->
			<task name="task3">
				<FileName>C:\Users\jayab\source\repos\hello-world\bin\Debug\net6.0\hello-world.exe</FileName>
			</task>
		</job>
		<job name="job2" active="true" minutely="1" timeZone="Mountain Standard Time">
			<task name="task1" active="true">
				<FileName>C:\Users\jayab\source\repos\hello-world\bin\Debug\net6.0\hello-world.exe</FileName>
				<Arguments>dir test1 test2 test3</Arguments>
			</task>
		</job>
	</group>
	<group name="group2">
		<job name="job3" active="true" minutely="1" timeZone="Mountain Standard Time">
			<task name="task1" active="true">
				<FileName>C:\Users\jayab\source\repos\hello-world\bin\Debug\net6.0\hello-world.exe</FileName>
				<Arguments>dir test1 test2 test3</Arguments>
			</task>
		</job>
	</group>
</root>
```
