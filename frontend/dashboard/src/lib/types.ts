export interface Campaign {
  id: string;
  name: string;
  dailyBudget: number;
  targetCtr: number;
  targetCpm: number;
  periodStart: string;
  periodEnd: string;
}

export interface CampaignMetrics {
  campaignId: string;
  campaignName: string;
  impressions: number;
  clicks: number;
  conversions: number;
  spendToDate: number;
  dailyBudget: number;
  targetCtr: number;
  targetCpm: number;
  ctr: number;
  cpm: number;
  conversionRate: number;
  budgetPacingPct: number;
  periodStart: string;
  periodEnd: string;
}

export interface SuggestedChange {
  field: string;
  from: unknown;
  to: unknown;
}

export interface AgentRecommendation {
  campaignId: string;
  action: RecommendedAction | number | `${number}`;
  urgency: 'low' | 'medium' | 'high' | 'critical';
  reason: string;
  suggestedChange: SuggestedChange | null;
  generatedAt: string;
}

export type RecommendedAction =
  | 'NoAction'
  | 'PauseCreative'
  | 'ReallocateBudget'
  | 'IncreaseBid'
  | 'DecreaseBid'
  | 'FlagAnomaly';
