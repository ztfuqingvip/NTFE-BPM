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
using NHibernate;

namespace Taobao.Workflow.Activities.Repositories
{
    /// <summary>
    /// 流程仓储
    /// </summary>
    public class ProcessRepository : NHibernateRepositoryBase<Guid, Process>, IProcessRepository
    {
        #region IProcessRepository Members

        public IEnumerable<Process> FindProcesses(string key, int? pageIndex, int? pageSize, ProcessStatus[] status, out long total)
        {
            return this.FindAll(pageIndex
                , pageSize
                , new Order[] { Order.Desc("CreateTime") }
                , new ICriterion[] { Expression.Like("Title", key), Expression.In("Status", status) }//TODO:完善流程搜索
                , out total);
        }

        public IEnumerable<Process> FindProcesses(object criteria, int? pageIndex, int? pageSize, out long total)
        {
            return this.FindAll(criteria as NHibernate.Criterion.DetachedCriteria, pageIndex, pageSize, out total);
        }

        public IEnumerable<Process> FindSubProcesses(Process parent)
        {
            return this.FindAll(Expression.Eq("ParentProcessId", parent.ID));
        }

        [Transaction(TransactionMode.Requires)]
        public void AddActivityInstance(ActivityInstanceBase instance)
        {
            this.GetSession().Save(instance);
        }

        [Transaction(TransactionMode.Requires)]
        public void UpdateActivityInstance(ActivityInstanceBase instance)
        {
            this.GetSession().Update(instance);
        }

        public ActivityInstanceBase FindActivityInstance(long id)
        {
            return this.GetSession().CreateCriteria<ActivityInstanceBase>()
                .Add(Expression.Eq("ID", id))
                .UniqueResult<ActivityInstanceBase>();
        }

        public ActivityInstanceBase FindActivityInstance(Process process, long workflowActivityInstanceId)
        {
            var list = this.GetSession().CreateCriteria<ActivityInstanceBase>()
                .Add(Expression.Eq("ProcessId", process.ID))
                .Add(Expression.Eq("_workflowActivityInstanceId", workflowActivityInstanceId))
                .AddOrder(Order.Desc("CreateTime"))//HACK:根据工作流活动实例时间倒序查找
                .List<ActivityInstanceBase>();
            return list.Count > 0 ? list[0] : null;
        }

        public SubProcessActivityInstance FindSubProcessActivityInstance(Process process, Process sub)
        {
            return this.GetSession().CreateCriteria<SubProcessActivityInstance>()
                .Add(Expression.Eq("ProcessId", process.ID))
                .Add(Expression.Eq("SubProcessId", sub.ID))
                .UniqueResult<SubProcessActivityInstance>();
        }

        public IEnumerable<ActivityInstanceBase> FindAllActivityInstances(Process process)
        {
            return this.GetSession()
                .CreateCriteria<ActivityInstanceBase>()
                .Add(Expression.Eq("ProcessId", process.ID))
                .List<ActivityInstanceBase>();
        }

        public string FindWorkflowInstanceData(Guid id)
        {
            return this.GetSession().CreateSQLQuery("select WorkflowData from NTFE_Process where id=:processId")
                .SetGuid("processId", id)
                .UniqueResult<string>();
        }

        public void UpdateWorkflowInstanceData(Guid id, string data)
        {
            this.GetSession().CreateSQLQuery("update NTFE_Process set WorkflowData=:data where id=:processId")
                .SetGuid("processId", id)
                //see:https://github.com/ali-ent/NTFE-BPM/issues/1
                .SetParameter("data", data, NHibernate.NHibernateUtil.StringClob)
                //.SetString("data", data)
                .ExecuteUpdate();
        }

        #endregion
    }
}