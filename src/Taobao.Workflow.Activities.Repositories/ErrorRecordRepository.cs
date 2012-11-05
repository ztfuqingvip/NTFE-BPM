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
using Taobao.Workflow.Activities.Hosting;
using Castle.Services.Transaction;
using NHibernate.Criterion;

namespace Taobao.Workflow.Activities.Repositories
{
    public class ErrorRecordRepository : NHibernateRepositoryBase<long, ErrorRecord>, IErrorRecordRepository
    {
        public static readonly string Table_Error = "NTFE_ErrorRecord";

        #region IErrorRecordRepository Members

        [Transaction(TransactionMode.Requires)]
        public void CancelAll(Process process)
        {
            this.GetSession().CreateSQLQuery(string.Format(@"
update {0}
set IsDeleted=:isDeleted
where ProcessId=:processId", Table_Error))
                .SetBoolean("isDeleted", true)
                .SetGuid("processId", process.ID)
                .ExecuteUpdate();
        }

        [Transaction(TransactionMode.Requires)]
        public void CancelAll(Process process, string activityName)
        {
            this.GetSession().CreateSQLQuery(string.Format(@"
update {0}
set IsDeleted=:isDeleted
where ProcessId=:processId
and ActivityName=:activityName", Table_Error))
                .SetBoolean("isDeleted", true)
                .SetGuid("processId", process.ID)
                .SetString("activityName", activityName)
                .ExecuteUpdate();
        }

        public IEnumerable<ErrorRecord> FindAllValid()
        {
            return this.FindAll(Expression.Eq("_isDeleted", false));
        }

        public IEnumerable<ErrorRecord> FindAllValid(Process process)
        {
            return this.FindAll(Expression.Eq("_isDeleted", false), Expression.Eq("Process", process));
        }

        #endregion
    }
}