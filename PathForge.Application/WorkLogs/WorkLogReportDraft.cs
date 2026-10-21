namespace PathForge.Application.WorkLogs;

public sealed record WorkLogReportDraft(string? WhatWasDone, string? Result, string? Learned, string? Blocker, string? NextStep);
