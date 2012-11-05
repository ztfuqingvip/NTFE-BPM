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

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 节点实例事件
    /// </summary>
    public class ActivityInstanceArgs
    {
        /// <summary>
        /// 节点实例
        /// </summary>
        public HumanActivityInstance Instance { get; set; }
        /// <summary>
        /// 流程实例
        /// </summary>
        public Process Process { get; set; }

        public ActivityInstanceArgs(HumanActivityInstance instance, Process process)
        {
            this.Instance = instance;
            this.Process = process;
        }
    }
}