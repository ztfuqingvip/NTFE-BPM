/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeSharp.Core.Castles;
using NHibernate.Criterion;
using Castle.Services.Transaction;
using CodeSharp.Core.Services;
using Castle.Facilities.NHibernateIntegration;
using CodeSharp.Core;
using NHibernate;

namespace Taobao.Workflow.Activities.Repositories
{
    /// <summary>
    /// 任务仓储
    /// </summary>
    public class WorkItemRepository : NHibernateRepositoryBase<long, WorkItem>, IWorkItemRepository
    {
        public static readonly string Table_WorkItem = "NTFE_WorkItem";
        //private ILog _log;
        //public WorkItemRepository(ILoggerFactory factory)
        //{
        //    this._log = factory.Create(typeof(WorkItemRepository));
        //}

        #region 只允许获取有效的任务
        public override WorkItem FindBy(long key)
        {
            return this.FindOne(Expression.Eq("ID", key)
                , Expression.In("Status", WorkItemService.VALID_STATUS));
        }
        public override IEnumerable<WorkItem> FindAll()
        {
            return base.FindAll(Expression.In("Status"
                , WorkItemService.VALID_STATUS));
        }
        public override IEnumerable<WorkItem> FindBy(params long[] keys)
        {
            return base.FindAll(Expression.In("ID", keys)
                , Expression.In("Status", WorkItemService.VALID_STATUS));
        }
        #endregion

        #region IWorkItemRepository Members
        public IEnumerable<WorkItem> FindAllBy(object criteria, int? pageIndex, int? pageSize, out long total)
        {
            return this.FindAll(criteria as NHibernate.Criterion.DetachedCriteria, pageIndex, pageSize, out total);
        }
        public IEnumerable<WorkItem> FindAllBy(User user, params WorkItemStatus[] status)
        {
            return status == null || status.Length == 0
                ? this.FindAll(Expression.Eq("Actioner", user))
                : this.FindAll(Expression.Eq("Actioner", user), Expression.In("Status", status));
        }
        public IEnumerable<WorkItem> FindAllBy(User user, string[] processTypeNames, params WorkItemStatus[] status)
        {
            return status == null || status.Length == 0
                ? this.FindAll(Expression.Eq("Actioner", user), Expression.In("_processTypeName", processTypeNames))
                : this.FindAll(Expression.Eq("Actioner", user), Expression.In("_processTypeName", processTypeNames), Expression.In("Status", status));
        }
        public IEnumerable<WorkItem> FindAllBy(User user, Process process, string activityName)
        {
            return string.IsNullOrEmpty(activityName)
                ? this.FindAll(Expression.Eq("Actioner", user)
                , Expression.Eq("Process", process)
                , Expression.In("Status", WorkItemService.VALID_STATUS))
                : this.FindAll(Expression.Eq("Actioner", user)
                , Expression.Eq("Process", process)
                , Expression.In("Status", WorkItemService.VALID_STATUS)
                , Expression.Eq("ActivityName", activityName));
        }
        public IEnumerable<WorkItem> FindAllByProcess(Process process)
        {
            return this.FindAll(Expression.Eq("Process", process)
                , Expression.In("Status", WorkItemService.VALID_STATUS));
        }

        public IEnumerable<WorkItem> FindAllByProcessAndActivity(Process process, string activityName)
        {
            return this.FindAll(Expression.Eq("Process", process)
                , Expression.Eq("ActivityName", activityName)
                , Expression.In("Status", WorkItemService.VALID_STATUS));
        }
        //按Name查询
        public IEnumerable<WorkItem> FindAllByProcessType(string typeName)
        {
            return this.FindAll(Expression.Eq("_processTypeName", typeName)
             , Expression.In("Status", WorkItemService.VALID_STATUS));
        }
        //允许获取任何状态的任务用于计算
        //不允许声明独立事务 udplock、rowlock
        public IEnumerable<WorkItem> FindWorkItemsByActivityInstance(Process process, HumanActivityInstance humanActivityInstance)
        {
            return this.GetSession()
                .CreateCriteria<WorkItem>()
                .SetLockMode(LockMode.Upgrade)
                .SetCacheable(false)
                .Add(Expression.Eq("Process", process))
                .Add(Expression.Eq("ActivityInstance", humanActivityInstance)).List<WorkItem>();
        }
        public IEnumerable<WorkItem> FindWorkItemsByActivityInstance(WorkItem workItem)
        {
            this.GetSession().Evict(workItem);
            return this.FindWorkItemsByActivityInstance(workItem.Process, workItem.ActivityInstance);
        }

        [Transaction(TransactionMode.Requires)]
        public void CancelAll(Process process)
        {
            this.GetSession().CreateSQLQuery(string.Format(@"
update {0}
set Status=:status
where ProcessId=:processId", Table_WorkItem))
                .SetEnum("status", WorkItemStatus.Canceled)
                .SetGuid("processId", process.ID)
                .ExecuteUpdate();
        }

        [Transaction(TransactionMode.Requires)]
        public void CancelAll(Process process, long humanActivityInstanceId)
        {
            this.GetSession().CreateSQLQuery(string.Format(@"
update {0}
set Status=:status
where ProcessId=:processId and HumanActivityInstanceId=:activityInstanceId", Table_WorkItem))
                .SetEnum("status", WorkItemStatus.Canceled)
                .SetGuid("processId", process.ID)
                .SetInt64("activityInstanceId", humanActivityInstanceId)
                .ExecuteUpdate();
        }

        #endregion
    }
}