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
using Taobao.Activities.Statements;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 节点的完成规则
    /// </summary>
    public class FinishRule
    {
        private static readonly CodeSharp.Core.Utils.Serializer _serializer = new CodeSharp.Core.Utils.Serializer();
        //all of/at most of/at least of/and
        private string _scripts { get; set; }
        /// <summary>
        /// 获取完成规则脚本
        /// </summary>
        public virtual IDictionary<string, string> Scripts
        {
            get { return _serializer.JsonDeserialize<IDictionary<string, string>>(this._scripts); }
        }

        protected FinishRule() { }
        /// <summary>
        /// 节点的完成规则
        /// </summary>
        /// <param name="scripts"></param>
        public FinishRule(IDictionary<string, string> scripts)
        {
            this._scripts = _serializer.JsonSerialize(scripts);
        }
    }
}
