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
using System.Text.RegularExpressions;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 获取用户
    /// </summary>
    public class GetUsers : NativeActivity<string[]>, IActionersHelper
    {
        public string[] Scripts { get; private set; }
        public GetUsers(params string[] scripts)
        {
            this.Scripts = scripts;

            if (this.Scripts == null || this.Scripts.Length == 0)
                throw new InvalidOperationException("至少需要提供一个脚本");
        }

        protected override string[] Execute(NativeActivityContext context)
        {
            var parser = context.Resolve<IScriptParser>();
            var extension = context.GetExtension<DataFieldExtension>();
            var users = new List<string>();

            foreach (var script in this.Scripts)
            {
                //解析规则脚本
                var scriptUsers = parser.EvaluateUsers(script, extension);
                if (scriptUsers != null)
                    users.AddRange(scriptUsers);
            }
            //HACK:外部获取的用户不区分小写用户名
            return users.Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.ToLower())
                .Distinct()
                .ToArray();
        }

        #region IActionersHelper Members

        public string[] GetActioners(NativeActivityContext context)
        {
            return this.Execute(context);
        }

        #endregion
    }
}