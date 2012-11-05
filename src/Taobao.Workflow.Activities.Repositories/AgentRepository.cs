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

namespace Taobao.Workflow.Activities.Repositories
{
    /// <summary>
    /// 用户仓储
    /// </summary>
    public class AgentRepository : NHibernateRepositoryBase<Guid, Agent>, IAgentRepository
    {
        #region IAgentRepository Members
        public IEnumerable<Agent> FindAllBy(User user)
        {
            return this.FindAll(Expression.Eq("ActAs", user), Expression.Eq("_enable", true));
        }
        public IEnumerable<Agent> FindActings(User user)
        {
            return this.FindAll(Expression.Eq("User", user), Expression.Eq("_enable", true));
        }
        public IEnumerable<Agent> FindHistory(User user, int pageIndex, int pageSize, out long totalCount)
        {
            return this.FindAll(pageIndex
                , pageSize
                , new Order[] { Order.Desc("CreateTime") }
                , new ICriterion[] { Expression.Eq("User", user), Expression.Eq("_enable", false) }
                , out totalCount);
        }
        [Transaction(TransactionMode.Requires)]
        public void RevokeAll(User user)
        {
            throw new NotImplementedException();
            //暂不使用
            using (var session = this.GetSession())
                session.CreateSQLQuery("update ").ExecuteUpdate();
        }

        #endregion
    }
}