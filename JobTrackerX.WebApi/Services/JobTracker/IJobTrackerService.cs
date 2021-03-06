﻿using JobTrackerX.SharedLibs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobTrackerX.WebApi.Services.JobTracker
{
    public interface IJobTrackerService
    {
        Task<JobEntity> GetJobByIdAsync(long id);

        Task<JobTreeStatistics> GetJobStatisticsByIdAsync(long id);

        Task<List<JobEntity>> GetDescendantEntitiesAsync(long id);

        Task<IList<long>> GetDescendantIdsAsync(long id);

        Task<JobEntity> AddNewJobAsync(AddJobDto dto);

        Task<string> UpdateJobStatusAsync(long id, UpdateJobStateDto dto);

        Task<string> UpdateJobOptionsAsync(long id, UpdateJobOptionsDto dto);

        Task<List<JobEntity>> GetChildrenEntitiesAsync(long id);

        Task<List<JobEntity>> BatchGetJobEntitiesAsync(IEnumerable<long> jobIds);
        Task<long> GetNextIdAsync();
        Task AppendToJobLogAsync(long id, AppendLogDto dto);

        Task<string> GetJobLogAsync(long id);

        Task<string> GetJobLogUrlAsync(long id);
        Task<Dictionary<long, JobTreeStatistics>> GetJobStatisticsListByIdsAsync(IEnumerable<long> ids);
        Task<List<AddJobErrorResult>> BatchAddJobAsync(BatchAddJobDto batchAddJobDto);
        Task<long> GetDescendantsCountAsync(long jobId);
        Task<JobState> GetJobStateAsync(long jobId);
        Task<JobEntityLite> GetJobEntityLiteAsync(long jobId);
    }
}