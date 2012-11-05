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
    /// 流程类型仓储
    /// </summary>
    public interface IProcessTypeRepository : CodeSharp.Core.RepositoryFramework.IRepository<Guid, ProcessType>
    {
        /// <summary>
        /// 查找当前版本的流程类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ProcessType FindCurrentProcessType(string name);
        /// <summary>
        /// 查找指定版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        ProcessType FindByVersion(string name, string version);
        /// <summary>
        /// 查找流程类型的所有记录
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IEnumerable<ProcessType> FindAll(string name);
        /// <summary>
        /// 查找流程类型的历史发布记录
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IEnumerable<ProcessType> FindHistories(string name);
        /// <summary>
        /// 查找所有当前版本的流程类型
        /// </summary>
        /// <returns></returns>
        IEnumerable<ProcessType> FindAllCurrent();
        /// <summary>
        /// 取消指定流程类型的当前版本
        /// </summary>
        /// <param name="name"></param>
        void CancelCurrentProcessType(string name);
    }
}
