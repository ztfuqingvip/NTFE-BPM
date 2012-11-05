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
using CodeSharp.Core.RepositoryFramework;
using Castle.Services.Transaction;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 代理人服务对外接口
    /// </summary>
    public interface IAgentService
    {
        /// <summary>
        /// 获取指定的代理信息
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        Agent GetAgent(Guid agentId);
        /// <summary>
        /// 获取指定用户当前的代理信息
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        IEnumerable<Agent> GetAgents(User user);
        /// <summary>
        /// 获取指定用户被他人设为代理人（扮演）的列表
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        IEnumerable<Agent> GetActings(User user);
        /// <summary>
        /// 获取指定用户历史代理信息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalCount"></param>
        /// <returns></returns>
        IEnumerable<Agent> GetHistory(User user, int pageIndex, int pageSize, out long totalCount);
        /// <summary>
        /// 创建代理人信息
        /// </summary>
        /// <param name="agent"></param>
        void Create(Agent agent);
        /// <summary>
        /// 撤销/删除代理人
        /// </summary>
        /// <param name="agent"></param>
        void Revoke(Agent agent);
        /// <summary>
        /// 撤销/删除用户的所有代理人设置
        /// </summary>
        /// <param name="user"></param>
        void RevokeAll(User user);
    }
    /// <summary>
    /// 代理人服务
    /// </summary>
    [Transactional]
    public class AgentService : IAgentService
    {
        private static IAgentRepository _repository;
        static AgentService()
        {
            _repository = RepositoryFactory.GetRepository<IAgentRepository, Guid, Agent>();
        }

        #region IAgentService Members
        Agent IAgentService.GetAgent(Guid agentId)
        {
            var agent = _repository.FindBy(agentId);
            return agent != null && agent._enable ? agent : null;
        }
        IEnumerable<Agent> IAgentService.GetAgents(User user)
        {
            return _repository.FindAllBy(user);
        }
        IEnumerable<Agent> IAgentService.GetActings(User user)
        {
            return _repository.FindActings(user);
        }
        IEnumerable<Agent> IAgentService.GetHistory(User user, int pageIndex, int pageSize, out long totalCount)
        {
            return _repository.FindHistory(user, pageIndex, pageSize, out totalCount);
        }
        [Transaction(TransactionMode.Requires)]
        void IAgentService.Create(Agent agent)
        {
            if ((this as IAgentService).GetAgents(agent.ActAs)
                .FirstOrDefault(o => o._enable && o.User.Equals(agent.User)) != null)
            {
                throw new InvalidOperationException(string.Format("不能为{0}设置重复的代理人{1}"
                    , agent.ActAs.UserName
                    , agent.User.UserName));
            }

            _repository.Add(agent);
        }
        [Transaction(TransactionMode.Requires)]
        void IAgentService.Revoke(Agent agent)
        {
            agent._enable = false;
            _repository.Update(agent);
        }
        [Transaction(TransactionMode.Requires)]
        void IAgentService.RevokeAll(User user)
        {
            (this as IAgentService).GetAgents(user).ToList().ForEach(o => (this as IAgentService).Revoke(o));
            //_repository.RevokeAll(user);
        }

        #endregion
    }
}