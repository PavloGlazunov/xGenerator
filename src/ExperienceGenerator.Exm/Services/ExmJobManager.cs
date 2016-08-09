namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using ExperienceGenerator.Exm.Models;
  using Sitecore.Jobs;

  public class ExmJobManager
  {
    public static ExmJobManager Instance;

    private readonly List<ExmJobDefinition> jobDefinitions = new List<ExmJobDefinition>();

    public ExmJob StartJob(ExmJobDefinition jobDefinition)
    {
      jobDefinition.Job = new ExmJob();
      this.jobDefinitions.Add(jobDefinition);
      var options = new JobOptions("ExperienceGeneratorExm-" + jobDefinition.Job.Id, "ExperienceGenerator", Sitecore.Context.Site.Name, this, "Run", new object[] { jobDefinition });
      JobManager.Start(options);
      return jobDefinition.Job;
    }

    public void Run(ExmJobDefinition jobDefinition)
    {
      ExmEventsGenerator.Errors = 0;
      jobDefinition.Job.JobStatus = ExperienceGenerator.JobStatus.Running;
      foreach (var keyValuePair in jobDefinition)
      {
        var exmDataPreparationService = new ExmDataProcessingService(keyValuePair.Value, keyValuePair.Key);
        exmDataPreparationService.StatusChanged += this.JobStatusChanged;
        exmDataPreparationService.CreateData();
      }


      jobDefinition.Job.JobStatus = ExperienceGenerator.JobStatus.Complete;
    }

    private void JobStatusChanged(Guid campaignId, ExperienceGenerator.JobStatus status, string message)
    {
#warning does not work with multiple campaigns
      var exmJob = this.jobDefinitions.FirstOrDefault(x => x.ContainsKey(campaignId))?.Job;
      if (exmJob == null) return;
      exmJob.JobStatus = status;
      exmJob.Status = message;
      var match = Regex.Match(message, "Generating events for contact (\\d*) of (\\d*)");
      if (match.Success)
      {
        exmJob.CompletedContacts = int.Parse(match.Groups[1].Value);
        exmJob.TotalContacts = int.Parse(match.Groups[2].Value);

      }

    }

    public ExmJob Poll(Guid id)
    {
      var spec = this.jobDefinitions.FirstOrDefault(x => x.Job.Id == id);
      return spec == null ? null : spec.Job;
    }
  }
}