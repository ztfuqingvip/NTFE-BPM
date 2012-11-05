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

namespace Taobao.Workflow.Activities.Client
{
    /// <summary>
    /// 代理信息
    /// </summary>
    public class Agent
    {
        /// <summary>
        /// 代理人
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 被代理人/被扮演者
        /// </summary>
        public string ActAsUserName { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 代理生效时间
        /// </summary>
        public DateTime BeginTime { get; set; }
        /// <summary>
        /// 代理失效时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 代理范围
        /// </summary>
        public ActingRange Range { get; set; }
        /// <summary>
        /// 代理的流程类型
        /// </summary>
        public string[] ProcessTypeNames { get; set; }
    }
}
