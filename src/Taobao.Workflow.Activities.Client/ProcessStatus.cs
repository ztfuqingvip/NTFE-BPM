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
    /// 流程状态
    /// </summary>
    public enum ProcessStatus
    {
        /// <summary>
        /// 新建
        /// </summary>
        New = 0,
        /// <summary>
        /// 发生错误
        /// </summary>
        Error = 1,
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 2,
        /// <summary>
        /// 活动
        /// </summary>
        Active = 3,
        /// <summary>
        /// 完成
        /// </summary>
        Completed = 4,
        /// <summary>
        /// 被停止
        /// </summary>
        Stopped = 5,
        /// <summary>
        /// 被删除
        /// </summary>
        Deleted = 6
    }
}