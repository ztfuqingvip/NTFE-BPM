using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Taobao.Workflow.Activities.Test.Stub
{
    /// <summary>
    /// 用于常规测试的用户库访问桩模块
    /// </summary>
    public class UserHelper : Taobao.Workflow.Activities.Application.IUserHelper
    {
        public static readonly string Superior = "xiexun";

        #region IUserHelper Members

        public string GetSuperior(string user)
        {
            return Superior;
        }

        #endregion
    }
}
