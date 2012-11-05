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
using System.Activities.Presentation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Activities;
using System.Activities.Statements;

namespace Taobao.Workflow.Activities.Designer.Statements
{
    /// <summary>
    /// 并发包
    /// </summary>
    public class ParallelActivity : IActivityTemplateFactory
    {
        public Activity Create(DependencyObject target)
        {
            var parallel = new Parallel();
            parallel.DisplayName = "";
            return parallel;
        }
    }
}
