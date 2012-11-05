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
    /// 用于记录流程运行中产生的Server服务端节点信息
    /// <remarks>同时用于描述服务端节点实例</remarks>
    /// </summary>
    public class ServerActivityInstance : ActivityInstanceBase
    {
        protected ServerActivityInstance() : base() { }
        public ServerActivityInstance(Guid processId
            , int flowNodeIndex
            , long workflowActivityInstanceId
            , string activityName)
            : base(processId
            , flowNodeIndex
            , workflowActivityInstanceId
            , activityName) { }
    }
}