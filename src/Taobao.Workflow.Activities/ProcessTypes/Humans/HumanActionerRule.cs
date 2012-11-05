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
using System.Linq.Expressions;
using Taobao.Activities;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 人工节点的执行人规则
    /// </summary>
    public class HumanActionerRule
    {
        private static readonly CodeSharp.Core.Utils.Serializer _serializer = new CodeSharp.Core.Utils.Serializer();

        private string _scripts { get; set; }
        /// <summary>
        /// 获取执行人的脚本规则
        /// </summary>
        public virtual string[] Scripts
        {
            get { return _serializer.JsonDeserialize<string[]>(this._scripts); }
        }

        protected HumanActionerRule() { }

        /// <summary>
        /// 初始化人工节点的执行人规则
        /// </summary>
        /// <param name="scripts"></param>
        public HumanActionerRule(params string[] scripts)
        {
            if (scripts == null || scripts.Length == 0)
                throw new InvalidOperationException("至少为ActionerRule提供一个script");
            this._scripts = _serializer.JsonSerialize(scripts);
        }
    }
}
