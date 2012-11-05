using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Taobao.Workflow.Activities.Managements
{
    /// <summary>
    /// 用于管理的流程服务接口
    /// </summary>
    public interface IProcessService
    {
        void Goto(string activityName);
    }
}