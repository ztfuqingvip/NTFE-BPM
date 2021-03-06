/****** 对象:  Table [dbo].[NTFE_ErrorRecord]    脚本日期: 05/04/2012 18:41:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NTFE_ErrorRecord](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[CreateTime] [datetime] NULL,
	[Reason] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
	[ProcessId] [uniqueidentifier] NULL,
	[ErrorType] [nvarchar](50) NULL,
	[BookmarkName] [nvarchar](50) NULL,
	[ActivityName] [nvarchar](50) NULL,
	[ResumptionId] [bigint] NULL,
 CONSTRAINT [PK_NTFE_ErrorRecord] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

--数据表变更在以下追加变化
--201203 父子流程
ALTER TABLE [dbo].[NTFE_Process]
	ADD [ParentProcessId] [uniqueidentifier] NULL
ALTER TABLE [dbo].[NTFE_Activity]
	ADD	[SubProcessTypeName] [nvarchar](50) NULL
--20120423 扩充节点实例信息
ALTER TABLE dbo.NTFE_ActivityInstance ADD
	ActivityInstanceType nvarchar(50) NULL,
	SubProcessId uniqueidentifier NULL
ALTER TABLE dbo.NTFE_WaitingResumption ADD
	SubProcessActivityInstanceId bigint NULL,
	ActivityInstanceId bigint NULL
--20120503 升级规则
ALTER TABLE [dbo].[NTFE_Activity] ADD 
	[EscalationRule_ExpirationMinutes] [float] NULL,
	[EscalationRule_NotifyRepeatMinutes] [float] NULL,
	[EscalationRule_NotifyTemplateName] [nvarchar](100) NULL,
	[EscalationRule_GotoActivityName] [nvarchar](100) NULL,
	[EscalationRule_RedirectTo] [nvarchar](50) NULL
--20120504 错误重试细粒度化
ALTER TABLE dbo.NTFE_WaitingResumption ADD
	IsError bit NULL,
	SubProcessId uniqueidentifier NULL
--20120508 清理调整
ALTER TABLE dbo.NTFE_Activity
	DROP COLUMN CanHaveChildren, EscalationRule_AfterMinutes, EscalationRule_At
--20120515 调度相关
ALTER TABLE [dbo].[NTFE_Process]
	ADD [ChargingBy] [nvarchar](50) NULL
--20120523调整workitem的不必要冗余
ALTER TABLE dbo.NTFE_WorkItem
	DROP COLUMN ProcessTypeId, ReferredBookmarkName, FlowNodeIndex
	
	
--数据订正 
--ChargingBy预先生成
update NTFE_WaitingResumption set chargingby='ntfe01'
update NTFE_Process set chargingby='ntfe01'
--节点实例类型 原先只有human
update NTFE_ActivityInstance set ActivityInstanceType='human' where  ActivityInstanceType is null
--延迟调度项对应的流程状态都应为running
update NTFE_Process set Status=2 where id=(select ProcessId from NTFE_WaitingResumption where IsValid=1 and IsExecuted=0 and At is not null)



