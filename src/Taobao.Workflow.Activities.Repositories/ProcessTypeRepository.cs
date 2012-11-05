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
    /// 流程类型仓储
    /// </summary>
    public class ProcessTypeRepository : NHibernateRepositoryBase<Guid, ProcessType>, IProcessTypeRepository
    {
        public static readonly string Table_ProcessType = "NTFE_ProcessType";

        #region IProcessTypeRepository Members
        public ProcessType FindCurrentProcessType(string name)
        {
            return this.FindOne(Expression.Eq("IsCurrent", true), Expression.Eq("Name", name));
        }
        public IEnumerable<ProcessType> FindAll(string name)
        {
            return this.FindAll(Expression.Eq("Name", name));
        }
        public IEnumerable<ProcessType> FindHistories(string name)
        {
            return this.FindAll(Expression.Eq("IsCurrent", false), Expression.Eq("Name", name));
        }
        [Transaction(TransactionMode.Requires)]
        public void CancelCurrentProcessType(string name)
        {
            using (var session = this.GetSession())
                session.CreateSQLQuery(string.Format(
                    "update {0} set IsCurrent=:current where [Name]=:name", Table_ProcessType))
                    .SetBoolean("current", false)
                    .SetString("name", name)
                    .ExecuteUpdate();
        }

        public ProcessType FindByVersion(string name, string version)
        {
            return this.FindOne(Expression.Eq("Version", version), Expression.Eq("Name", name));
        }

        public IEnumerable<ProcessType> FindAllCurrent()
        {
            return this.FindAll(Expression.Eq("IsCurrent", true));
        }

        #endregion
    }
}