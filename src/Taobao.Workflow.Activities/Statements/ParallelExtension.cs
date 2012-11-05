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

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 为并行节点提供辅助功能
    /// </summary>
    public class ParallelExtension
    {
        private IList<long> _cancelledActivityInstanceIds;

        public ParallelExtension()
        {
            this._cancelledActivityInstanceIds = new List<long>();
        }
        /// <summary>
        /// 获取被取消的节点实例列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<long> GetCancelledActivityInstances()
        { 
            return this._cancelledActivityInstanceIds;
        } 
        /// <summary>
        /// 声明被取消的节点
        /// </summary>
        /// <param name="completedActivityInstanceIds"></param>
        public void Cancelled(params long[] completedActivityInstanceIds)
        {
            if (completedActivityInstanceIds != null)
                completedActivityInstanceIds.ToList().ForEach(o =>
                {
                    if (!this._cancelledActivityInstanceIds.Contains(o))
                        this._cancelledActivityInstanceIds.Add(o);
                });
        }
        /// <summary>
        /// 清除暂存信息
        /// </summary>
        public void Clear()
        {
            this._cancelledActivityInstanceIds.Clear();
        }
    }
}