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

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 代理人仓储
    /// </summary>
    public interface IAgentRepository : CodeSharp.Core.RepositoryFramework.IRepository<Guid, Agent>
    {
        /// <summary>
        /// 查找用户的代理人信息
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        IEnumerable<Agent> FindAllBy(User user);
        /// <summary>
        /// 查找用户的扮演信息
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        IEnumerable<Agent> FindActings(User user);
        /// <summary>
        /// 查找用户的历史代理记录
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalCount"></param>
        /// <returns></returns>
        IEnumerable<Agent> FindHistory(User user, int pageIndex, int pageSize, out long totalCount);
        /// <summary>
        /// 删除所有代理人设置
        /// </summary>
        /// <param name="user"></param>
        void RevokeAll(User user);
    }
}
