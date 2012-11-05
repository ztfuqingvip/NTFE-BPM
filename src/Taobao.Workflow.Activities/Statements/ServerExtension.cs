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
using Taobao.Activities;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 服务端活动扩展程序
    /// <remarks>主要用于暂存服务端活动运行时信息</remarks>
    /// </summary>
    public class ServerExtension
    {
        private IList<ServerActivityInstance> _instances { get; set; }

        public ServerExtension()
        {
            this._instances = new List<ServerActivityInstance>();
        }
        /// <summary>
        /// 添加服务端节点实例信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activityName"></param>
        /// <param name="flowNodeIndex"></param>
        public ServerActivityInstance AddServer(NativeActivityContext context, string activityName, int flowNodeIndex)
        {
            var server = new ServerActivityInstance(context.WorkflowInstanceId
                , flowNodeIndex
                , context.ActivityInstanceId
                , activityName);
            this._instances.Add(server);
            return server;
        }
        /// <summary>
        /// 获取暂存的服务端节点实例列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ServerActivityInstance> GetServerActivityInstances()
        {
            return this._instances.AsEnumerable();
        }
        /// <summary>
        /// 清除暂存信息
        /// </summary>
        public void Clear()
        {
            this._instances.Clear();
        }
    }
}
