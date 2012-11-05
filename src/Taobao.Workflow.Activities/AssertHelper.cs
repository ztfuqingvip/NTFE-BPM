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
    /// 定义重要的断言帮助
    /// </summary>
    public static class AssertHelper
    {
        public static void ThrowIfInvalidFlowNodeIndex(long index)
        {
            if (index < 0)
                throw new InvalidOperationException("FlowNodeIndex不合法");
        }
        public static void ThrowIfInvalidActivityInstanceId(long id)
        {
            if (id <= 0)
                throw new InvalidOperationException("ActivityInstanceId不合法");
        }

        //public static void ThrowIfSchedulingUnsafeProcessStatus(ProcessStatus status)
        //{
        //    if (status != ProcessStatus.Active 
        //        && status != ProcessStatus.Error 
        //        && status != ProcessStatus.Completed)
        //        throw new InvalidOperationException("流程处于非调度安全状态");
        //}
    }
}
