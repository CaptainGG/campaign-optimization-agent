import type { AgentRecommendation, RecommendedAction } from './types';

const ACTIONS: RecommendedAction[] = [
  'NoAction',
  'PauseCreative',
  'ReallocateBudget',
  'IncreaseBid',
  'DecreaseBid',
  'FlagAnomaly'
];

const ACTION_LABELS: Record<RecommendedAction, string> = {
  NoAction: 'No action',
  PauseCreative: 'Pause creative',
  ReallocateBudget: 'Reallocate budget',
  IncreaseBid: 'Increase bid',
  DecreaseBid: 'Decrease bid',
  FlagAnomaly: 'Flag anomaly'
};

const ACTION_ICONS: Record<RecommendedAction, string> = {
  NoAction: 'OK',
  PauseCreative: 'II',
  ReallocateBudget: '<>',
  IncreaseBid: '+',
  DecreaseBid: '-',
  FlagAnomaly: '!'
};

export function normalizeAction(action: AgentRecommendation['action']): RecommendedAction {
  if (typeof action === 'number') {
    return ACTIONS[action] ?? 'NoAction';
  }

  if (typeof action === 'string') {
    if ((ACTIONS as string[]).includes(action)) {
      return action as RecommendedAction;
    }

    const numeric = Number(action);
    if (Number.isInteger(numeric)) {
      return ACTIONS[numeric] ?? 'NoAction';
    }
  }

  return 'NoAction';
}

export function actionLabel(action: AgentRecommendation['action']): string {
  return ACTION_LABELS[normalizeAction(action)];
}

export function actionIcon(action: AgentRecommendation['action']): string {
  return ACTION_ICONS[normalizeAction(action)];
}

export function isNoAction(action: AgentRecommendation['action']): boolean {
  return normalizeAction(action) === 'NoAction';
}
