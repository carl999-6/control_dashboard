export type Goal = {
  id: string
  projectId: string
  title: string
  description: string
  metric: string
  targetValue: number | null
  status: string
  dueDate: string | null
}

export type Project = {
  id: string
  name: string
  slug: string
  domain: string
  type: string
  status: string
  timeZone: string
  environment: string
  description: string
  isDemoData: boolean
  goals: Goal[]
}

export type ProjectInput = Omit<Project, 'id' | 'slug' | 'isDemoData' | 'goals'>

export type GoalInput = {
  title: string
  description: string
  metric: string
  targetValue: number | null
  status: string
  dueDate: string | null
}

export type Preference = {
  id: number
  timeZone: string
  currency: string
  defaultDateRangeDays: number
  compactNotifications: boolean
  updatedAt: string
}

export type Session = {
  authenticated: boolean
  displayName: string | null
}

export type Campaign = {
  id: string
  projectId: string
  name: string
  objective: string
  status: string
  utmCampaign: string
  startDate: string | null
  endDate: string | null
  isDemoData: boolean
  createdAt: string
  updatedAt: string
}

export type CampaignInput = Pick<Campaign, 'name' | 'objective' | 'status' | 'utmCampaign' | 'startDate' | 'endDate'>

export type SocialPost = {
  id: string
  projectId: string
  campaignId: string | null
  platform: string
  topic: string
  format: string
  status: string
  externalUrl: string
  dataSource: string
  publishedAt: string | null
  impressions: number
  engagements: number
  clicks: number
  createdAt: string
  updatedAt: string
}

export type SocialPostInput = Pick<SocialPost, 'campaignId' | 'platform' | 'topic' | 'format' | 'status' | 'externalUrl' | 'publishedAt' | 'impressions' | 'engagements' | 'clicks'>

export type MarketingEventInput = {
  stage: string
  source: string
  medium: string
  landingPath: string
  count: number
  occurredAt: string | null
  campaignId: string | null
  socialPostId: string | null
  dataSource: string
}

export type MarketingSummary = {
  funnel: { stage: string; count: number }[]
  sources: { source: string; visits: number; contacts: number }[]
  campaigns: number
  posts: number
  impressions: number
  engagements: number
  clicks: number
  containsDemoData: boolean
}

export type PostHogStatus = {
  isConfigured: boolean
  keyAvailable: boolean
  isEnabled: boolean
  region: 'us' | 'eu'
  externalProjectId: number
  publicToken: string
  apiKeyEnvironmentVariable: string
  lookbackDays: number
  rowLimit: number
  lastSyncAt: string | null
  lastError: string
}

export type PostHogSettingsInput = Pick<PostHogStatus, 'region' | 'externalProjectId' | 'publicToken' | 'apiKeyEnvironmentVariable' | 'lookbackDays' | 'rowLimit' | 'isEnabled'> & { confirmReplaceData?: boolean }

export type PostHogMetric = {
  date: string
  event: string
  source: string
  medium: string
  campaign: string
  content: string
  path: string
  eventCount: number
  sessions: number
  xReplyProposalId: string | null
  campaignId: string | null
}

export type PostHogSummary = {
  pageviews: number
  sessions: number
  quoteRequests: number
  whatsAppClicks: number
  metrics: PostHogMetric[]
  attribution: { source: string; campaign: string; content: string; visits: number; pageviews: number; quoteRequests: number; whatsAppClicks: number; xReplyProposalId: string | null; campaignId: string | null }[]
  lastSyncAt: string | null
}

export type PostHogEventDetail = {
  occurredAt: string
  event: string
  path: string
  source: string
  medium: string
  campaign: string
  content: string
  attribution: 'utm' | 'referrer' | 'direct' | 'unknown'
  referrerDomain: string
  browser: string
  city: string
  country: string
  deviceType: string
  os: string
  osVersion: string
}

export type SeoOpportunity = {
  id: string
  projectId: string
  query: string
  targetPage: string
  evidence: string
  hypothesis: string
  status: string
  dataSource: string
  baselineImpressions: number
  baselineClicks: number
  isDemoData: boolean
  createdAt: string
  updatedAt: string
}

export type SeoOpportunityInput = Pick<SeoOpportunity, 'query' | 'targetPage' | 'evidence' | 'hypothesis' | 'status' | 'baselineImpressions' | 'baselineClicks'>

export type EditorialHistory = {
  id: string
  fromStatus: string
  toStatus: string
  note: string
  actor: string
  changedAt: string
}

export type ContentPiece = {
  id: string
  projectId: string
  seoOpportunityId: string | null
  title: string
  slug: string
  contentType: string
  primaryKeyword: string
  searchIntent: string
  hypothesis: string
  baselineSummary: string
  objective: string
  owner: string
  brief: string
  draftMarkdown: string
  metaTitle: string
  metaDescription: string
  status: string
  scheduledFor: string | null
  measuredAt: string | null
  resultImpressions: number | null
  resultClicks: number | null
  resultNotes: string
  wordPressEditUrl: string
  wordPressPostId: number | null
  wordPressStatus: string
  wordPressDraftCreatedAt: string | null
  isDemoData: boolean
  createdAt: string
  updatedAt: string
  history: EditorialHistory[]
}

export type ContentPieceInput = Pick<ContentPiece, 'seoOpportunityId' | 'title' | 'contentType' | 'primaryKeyword' | 'searchIntent' | 'hypothesis' | 'baselineSummary' | 'objective' | 'owner' | 'brief' | 'draftMarkdown' | 'metaTitle' | 'metaDescription' | 'scheduledFor'>

export type SeoSummary = {
  opportunities: number
  activePieces: number
  pendingReview: number
  scheduled: number
  measured: number
  containsDemoData: boolean
}

export type RatePlan = {
  id: string
  projectId: string
  provider: string
  model: string
  inputUsdPerMillion: number
  outputUsdPerMillion: number
  effectiveFrom: string
  isActive: boolean
  isDemoData: boolean
}

export type RatePlanInput = Pick<RatePlan, 'provider' | 'model' | 'inputUsdPerMillion' | 'outputUsdPerMillion'> & { effectiveFrom: string | null }

export type Budget = {
  id: string
  projectId: string
  dailyLimitUsd: number
  monthlyLimitUsd: number
  warningPercent: number
  exchangeRateGtqPerUsd: number
  isPaused: boolean
  updatedAt: string
}

export type BudgetInput = Omit<Budget, 'id' | 'projectId' | 'updatedAt'>

export type ExecutionAudit = {
  id: string
  eventType: string
  fromStatus: string
  toStatus: string
  note: string
  actor: string
  occurredAt: string
}

export type ExecutionRecord = {
  id: string
  projectId: string
  apiRatePlanId: string
  parentExecutionId: string | null
  idempotencyKey: string
  provider: string
  model: string
  flow: string
  status: string
  approvalRequired: boolean
  approvedBy: string
  approvedAt: string | null
  inputUnits: number
  outputUnits: number
  estimatedCostUsd: number
  estimatedCostGtq: number
  attemptNumber: number
  errorCode: string
  errorMessage: string
  isDemoData: boolean
  createdAt: string
  startedAt: string | null
  completedAt: string | null
  audit: ExecutionAudit[]
}

export type PlanExecutionInput = Pick<ExecutionRecord, 'idempotencyKey' | 'provider' | 'model' | 'flow' | 'inputUnits' | 'outputUnits' | 'approvalRequired'>

export type ExecutionSummary = {
  todayCostUsd: number
  monthCostUsd: number
  monthCostGtq: number
  monthBudgetUsd: number
  budgetUsedPercent: number
  awaitingApproval: number
  blocked: number
  failed: number
  succeeded: number
  isPaused: boolean
  providers: { provider: string; costUsd: number; costGtq: number; executions: number }[]
}

export type NotificationPolicy = {
  id: string
  projectId: string
  isEnabled: boolean
  deliveryMode: string
  minimumSeverity: string
  groupWindowMinutes: number
  quietHoursStart: number
  quietHoursEnd: number
  updatedAt: string
}

export type NotificationPolicyInput = Omit<NotificationPolicy, 'id' | 'projectId' | 'deliveryMode' | 'updatedAt'>

export type NotificationRecord = {
  id: string
  projectId: string
  deduplicationKey: string
  category: string
  severity: string
  title: string
  message: string
  flow: string
  provider: string
  model: string
  inputUnits: number | null
  outputUnits: number | null
  estimatedCostUsd: number | null
  status: string
  groupCount: number
  attemptCount: number
  simulateFailure: boolean
  errorMessage: string
  isDemoData: boolean
  createdAt: string
  updatedAt: string
  lastAttemptAt: string | null
  deliveredAt: string | null
}

export type DispatchNotificationInput = Pick<NotificationRecord, 'deduplicationKey' | 'category' | 'severity' | 'title' | 'message' | 'simulateFailure'>

export type AutomationSchedule = {
  id: string
  projectId: string
  workflow: string
  displayName: string
  isEnabled: boolean
  frequency: string
  intervalMinutes: number | null
  localTime: string
  dayOfWeek: number | null
  maxRunsPerDay: number
  nextRunAt: string | null
  lastRunAt: string | null
  updatedAt: string
}

export type AutomationScheduleInput = Pick<AutomationSchedule, 'isEnabled' | 'frequency' | 'intervalMinutes' | 'localTime' | 'dayOfWeek' | 'maxRunsPerDay'>

export type AutomationRun = {
  id: string
  projectId: string
  automationScheduleId: string
  trigger: string
  status: string
  errorCode: string
  errorMessage: string
  createdAt: string
  startedAt: string | null
  completedAt: string | null
}

export type SearchConsoleStatus = {
  clientConfigured: boolean
  connected: boolean
  status: string
  property: string
  permissionLevel: string
  lookbackDays: number
  rowLimit: number
  lastSyncAt: string | null
  lastError: string
}

export type SearchConsoleSummary = {
  rows: number
  clicks: number
  impressions: number
  ctr: number
  averagePosition: number
  startDate: string | null
  endDate: string | null
  topQueries: { query: string; page: string; clicks: number; impressions: number; ctr: number; position: number }[]
}

export type GeminiSettings = {
  apiKeyConfigured: boolean
  isEnabled: boolean
  model: string
  minimumImpressions: number
  minimumPosition: number
  maximumPosition: number
  maximumCtrPercent: number
  maximumSeoDraftsPerDay: number
  maximumXProposalsPerDay: number
  maximumTotalGeminiRunsPerDay: number
  minimumDraftWords: number
  maximumOutputTokens: number
  owner: string
  draftsGeneratedToday: number
  xProposalsGeneratedToday: number
  totalGeminiRunsToday: number
  updatedAt: string | null
}

export type GeminiSettingsInput = Omit<GeminiSettings, 'apiKeyConfigured' | 'draftsGeneratedToday' | 'xProposalsGeneratedToday' | 'totalGeminiRunsToday' | 'updatedAt'>

export type XAssistantSettings = {
  isEnabled: boolean
  apiReadEnabled: boolean
  bearerTokenConfigured: boolean
  searchQuery: string
  language: string
  maximumPostsPerSync: number
  readCostUsdPerPost: number
  maximumSyncCostUsd: number
  toneInstructions: string
  landingPath: string
  utmCampaign: string
  lastSyncAt: string | null
  lastError: string
}

export type XAssistantSettingsInput = Pick<XAssistantSettings, 'isEnabled' | 'apiReadEnabled' | 'searchQuery' | 'language' | 'maximumPostsPerSync' | 'readCostUsdPerPost' | 'toneInstructions' | 'landingPath' | 'utmCampaign'>

export type XSourcePost = {
  id: string
  externalPostId: string
  url: string
  authorUsername: string
  text: string
  language: string
  likeCount: number
  replyCount: number
  repostCount: number
  quoteCount: number
  impressionCount: number
  status: string
  dataSource: string
  postedAt: string
  importedAt: string
}

export type XSourcePostInput = Pick<XSourcePost, 'url' | 'authorUsername' | 'text' | 'language' | 'likeCount' | 'replyCount' | 'repostCount' | 'quoteCount' | 'impressionCount'> & { postedAt: string | null }

export type XReplyProposal = {
  id: string
  xSourcePostId: string
  sourceUrl: string
  sourceAuthor: string
  sourceText: string
  recommendedReply: string
  alternativeOne: string
  alternativeTwo: string
  selectedReply: string
  rationale: string
  riskNotes: string
  status: string
  publishedReplyUrl: string
  trackingUrl: string
  publishedAt: string | null
  createdAt: string
  updatedAt: string
}

export type WordPressSettings = {
  isEnabled: boolean
  passwordConfigured: boolean
  baseUrl: string
  username: string
  status: string
  lastCheckedAt: string | null
  lastError: string
}

export type WordPressSettingsInput = Pick<WordPressSettings, 'isEnabled' | 'baseUrl' | 'username'>

export type NotificationSummary = {
  delivered: number
  pending: number
  failed: number
  grouped: number
  suppressed: number
  totalOccurrences: number
}

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    ...options,
    headers: {
      ...(options?.body ? { 'Content-Type': 'application/json' } : {}),
      ...options?.headers,
    },
  })

  if (!response.ok) {
    let message = 'No se pudo completar la operación.'
    try {
      const payload = await response.json()
      const errors = payload.errors as Record<string, string[]> | undefined
      message = errors ? Object.values(errors).flat()[0] : payload.detail ?? payload.title ?? message
    } catch {
      // La respuesta puede no incluir JSON en errores de infraestructura.
    }
    throw new ApiError(message, response.status)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export const api = {
  getSession: () => request<Session>('/api/auth/session'),
  login: (password: string) => request<Session>('/api/auth/login', { method: 'POST', body: JSON.stringify({ password }) }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
  getProjects: () => request<Project[]>('/api/projects'),
  createProject: (project: ProjectInput) => request<Project>('/api/projects', { method: 'POST', body: JSON.stringify(project) }),
  updateProject: (id: string, project: ProjectInput) => request<Project>(`/api/projects/${id}`, { method: 'PUT', body: JSON.stringify(project) }),
  deleteProject: (id: string) => request<void>(`/api/projects/${id}`, { method: 'DELETE' }),
  createGoal: (projectId: string, goal: GoalInput) => request<Goal>(`/api/projects/${projectId}/goals`, { method: 'POST', body: JSON.stringify(goal) }),
  updateGoal: (projectId: string, goalId: string, goal: GoalInput) => request<Goal>(`/api/projects/${projectId}/goals/${goalId}`, { method: 'PUT', body: JSON.stringify(goal) }),
  deleteGoal: (projectId: string, goalId: string) => request<void>(`/api/projects/${projectId}/goals/${goalId}`, { method: 'DELETE' }),
  getSettings: () => request<Preference>('/api/settings'),
  updateSettings: (settings: Omit<Preference, 'id' | 'updatedAt'>) => request<Preference>('/api/settings', { method: 'PUT', body: JSON.stringify(settings) }),
  getCampaigns: (projectId: string) => request<Campaign[]>(`/api/projects/${projectId}/marketing/campaigns`),
  createCampaign: (projectId: string, campaign: CampaignInput) => request<Campaign>(`/api/projects/${projectId}/marketing/campaigns`, { method: 'POST', body: JSON.stringify(campaign) }),
  updateCampaign: (projectId: string, campaignId: string, campaign: CampaignInput) => request<Campaign>(`/api/projects/${projectId}/marketing/campaigns/${campaignId}`, { method: 'PUT', body: JSON.stringify(campaign) }),
  deleteCampaign: (projectId: string, campaignId: string) => request<void>(`/api/projects/${projectId}/marketing/campaigns/${campaignId}`, { method: 'DELETE' }),
  getSocialPosts: (projectId: string) => request<SocialPost[]>(`/api/projects/${projectId}/marketing/posts`),
  createSocialPost: (projectId: string, post: SocialPostInput) => request<SocialPost>(`/api/projects/${projectId}/marketing/posts`, { method: 'POST', body: JSON.stringify(post) }),
  updateSocialPost: (projectId: string, postId: string, post: SocialPostInput) => request<SocialPost>(`/api/projects/${projectId}/marketing/posts/${postId}`, { method: 'PUT', body: JSON.stringify(post) }),
  deleteSocialPost: (projectId: string, postId: string) => request<void>(`/api/projects/${projectId}/marketing/posts/${postId}`, { method: 'DELETE' }),
  getMarketingSummary: (projectId: string) => request<MarketingSummary>(`/api/projects/${projectId}/marketing/summary`),
  getPostHogStatus: (projectId: string) => request<PostHogStatus>(`/api/projects/${projectId}/integrations/posthog/status`),
  updatePostHogSettings: (projectId: string, settings: PostHogSettingsInput) => request<PostHogStatus>(`/api/projects/${projectId}/integrations/posthog/settings`, { method: 'PUT', body: JSON.stringify(settings) }),
  getPostHogSummary: (projectId: string, days = 28) => request<PostHogSummary>(`/api/projects/${projectId}/integrations/posthog/summary?days=${days}`),
  getPostHogEvents: (projectId: string, days: number, filters: { source?: string; campaign?: string; content?: string } = {}) => {
    const params = new URLSearchParams({ days: String(days) })
    if (filters.source) params.set('source', filters.source)
    if (filters.campaign) params.set('campaign', filters.campaign)
    if (filters.content) params.set('content', filters.content)
    return request<PostHogEventDetail[]>(`/api/projects/${projectId}/integrations/posthog/events?${params}`)
  },
  importMarketingEvents: (projectId: string, events: MarketingEventInput[]) => request<{ imported: number; totalCount: number }>(`/api/projects/${projectId}/marketing/events/import`, { method: 'POST', body: JSON.stringify({ events }) }),
  getSeoOpportunities: (projectId: string) => request<SeoOpportunity[]>(`/api/projects/${projectId}/seo/opportunities`),
  createSeoOpportunity: (projectId: string, item: SeoOpportunityInput) => request<SeoOpportunity>(`/api/projects/${projectId}/seo/opportunities`, { method: 'POST', body: JSON.stringify(item) }),
  updateSeoOpportunity: (projectId: string, itemId: string, item: SeoOpportunityInput) => request<SeoOpportunity>(`/api/projects/${projectId}/seo/opportunities/${itemId}`, { method: 'PUT', body: JSON.stringify(item) }),
  deleteSeoOpportunity: (projectId: string, itemId: string) => request<void>(`/api/projects/${projectId}/seo/opportunities/${itemId}`, { method: 'DELETE' }),
  getContentPieces: (projectId: string) => request<ContentPiece[]>(`/api/projects/${projectId}/seo/content`),
  createContentPiece: (projectId: string, item: ContentPieceInput) => request<ContentPiece>(`/api/projects/${projectId}/seo/content`, { method: 'POST', body: JSON.stringify(item) }),
  updateContentPiece: (projectId: string, itemId: string, item: ContentPieceInput) => request<ContentPiece>(`/api/projects/${projectId}/seo/content/${itemId}`, { method: 'PUT', body: JSON.stringify(item) }),
  deleteContentPiece: (projectId: string, itemId: string) => request<void>(`/api/projects/${projectId}/seo/content/${itemId}`, { method: 'DELETE' }),
  transitionContentPiece: (projectId: string, itemId: string, targetStatus: string, note: string, scheduledFor: string | null = null) => request<ContentPiece>(`/api/projects/${projectId}/seo/content/${itemId}/transition`, { method: 'POST', body: JSON.stringify({ targetStatus, note, scheduledFor }) }),
  sendWordPressDraft: (projectId: string, itemId: string) => request<ContentPiece>(`/api/projects/${projectId}/integrations/wordpress/content/${itemId}/draft`, { method: 'POST' }),
  measureContentPiece: (projectId: string, itemId: string, impressions: number, clicks: number, notes: string) => request<ContentPiece>(`/api/projects/${projectId}/seo/content/${itemId}/measurement`, { method: 'PUT', body: JSON.stringify({ impressions, clicks, notes, measuredAt: null }) }),
  getSeoSummary: (projectId: string) => request<SeoSummary>(`/api/projects/${projectId}/seo/summary`),
  getRatePlans: (projectId: string) => request<RatePlan[]>(`/api/projects/${projectId}/operations/rates`),
  createRatePlan: (projectId: string, item: RatePlanInput) => request<RatePlan>(`/api/projects/${projectId}/operations/rates`, { method: 'POST', body: JSON.stringify(item) }),
  getBudget: (projectId: string) => request<Budget>(`/api/projects/${projectId}/operations/budget`),
  updateBudget: (projectId: string, item: BudgetInput) => request<Budget>(`/api/projects/${projectId}/operations/budget`, { method: 'PUT', body: JSON.stringify(item) }),
  getExecutions: (projectId: string) => request<ExecutionRecord[]>(`/api/projects/${projectId}/operations/executions`),
  getExecutionSummary: (projectId: string) => request<ExecutionSummary>(`/api/projects/${projectId}/operations/summary`),
  planExecution: (projectId: string, item: PlanExecutionInput) => request<ExecutionRecord>(`/api/projects/${projectId}/operations/executions/plan`, { method: 'POST', body: JSON.stringify(item) }),
  approveExecution: (projectId: string, executionId: string, note: string) => request<ExecutionRecord>(`/api/projects/${projectId}/operations/executions/${executionId}/approve`, { method: 'POST', body: JSON.stringify({ note }) }),
  completeExecution: (projectId: string, executionId: string, succeeded: boolean) => request<ExecutionRecord>(`/api/projects/${projectId}/operations/executions/${executionId}/complete`, { method: 'POST', body: JSON.stringify({ succeeded, errorCode: succeeded ? null : 'SIMULATED_PROVIDER_ERROR', errorMessage: succeeded ? null : 'Fallo simulado para probar el control de reintentos.' }) }),
  retryExecution: (projectId: string, executionId: string) => request<ExecutionRecord>(`/api/projects/${projectId}/operations/executions/${executionId}/retry`, { method: 'POST' }),
  cancelExecution: (projectId: string, executionId: string) => request<ExecutionRecord>(`/api/projects/${projectId}/operations/executions/${executionId}/cancel`, { method: 'POST' }),
  getNotificationPolicy: (projectId: string) => request<NotificationPolicy>(`/api/projects/${projectId}/notifications/policy`),
  updateNotificationPolicy: (projectId: string, item: NotificationPolicyInput) => request<NotificationPolicy>(`/api/projects/${projectId}/notifications/policy`, { method: 'PUT', body: JSON.stringify(item) }),
  getNotifications: (projectId: string) => request<NotificationRecord[]>(`/api/projects/${projectId}/notifications`),
  getNotificationSummary: (projectId: string) => request<NotificationSummary>(`/api/projects/${projectId}/notifications/summary`),
  dispatchNotification: (projectId: string, item: DispatchNotificationInput) => request<NotificationRecord>(`/api/projects/${projectId}/notifications/dispatch`, { method: 'POST', body: JSON.stringify(item) }),
  retryNotification: (projectId: string, notificationId: string) => request<NotificationRecord>(`/api/projects/${projectId}/notifications/${notificationId}/retry`, { method: 'POST' }),
  processQueuedNotifications: (projectId: string) => request<NotificationRecord[]>(`/api/projects/${projectId}/notifications/process-queued`, { method: 'POST' }),
  getAutomationSchedules: (projectId: string) => request<AutomationSchedule[]>(`/api/projects/${projectId}/automations`),
  updateAutomationSchedule: (projectId: string, workflow: string, item: AutomationScheduleInput) => request<AutomationSchedule>(`/api/projects/${projectId}/automations/${workflow}`, { method: 'PUT', body: JSON.stringify(item) }),
  getAutomationRuns: (projectId: string) => request<AutomationRun[]>(`/api/projects/${projectId}/automations/runs`),
  runAutomation: (projectId: string, workflow: string) => request<AutomationRun>(`/api/projects/${projectId}/automations/${workflow}/run`, { method: 'POST' }),
  getSearchConsoleStatus: (projectId: string) => request<SearchConsoleStatus>(`/api/projects/${projectId}/integrations/search-console/status`),
  authorizeSearchConsole: (projectId: string) => request<{ authorizationUrl: string }>(`/api/projects/${projectId}/integrations/search-console/authorize`, { method: 'POST' }),
  updateSearchConsoleSettings: (projectId: string, lookbackDays: number, rowLimit: number) => request<SearchConsoleStatus>(`/api/projects/${projectId}/integrations/search-console/settings`, { method: 'PUT', body: JSON.stringify({ lookbackDays, rowLimit }) }),
  getSearchConsoleSummary: (projectId: string) => request<SearchConsoleSummary>(`/api/projects/${projectId}/integrations/search-console/summary`),
  disconnectSearchConsole: (projectId: string, confirmation: string) => request<void>(`/api/projects/${projectId}/integrations/search-console/connection`, { method: 'DELETE', body: JSON.stringify({ confirmation }) }),
  getGeminiSettings: (projectId: string) => request<GeminiSettings>(`/api/projects/${projectId}/integrations/gemini/settings`),
  updateGeminiSettings: (projectId: string, settings: GeminiSettingsInput) => request<GeminiSettings>(`/api/projects/${projectId}/integrations/gemini/settings`, { method: 'PUT', body: JSON.stringify(settings) }),
  getWordPressSettings: (projectId: string) => request<WordPressSettings>(`/api/projects/${projectId}/integrations/wordpress/settings`),
  updateWordPressSettings: (projectId: string, settings: WordPressSettingsInput) => request<WordPressSettings>(`/api/projects/${projectId}/integrations/wordpress/settings`, { method: 'PUT', body: JSON.stringify(settings) }),
  testWordPressConnection: (projectId: string) => request<{ succeeded: boolean; status: string; message: string }>(`/api/projects/${projectId}/integrations/wordpress/test`, { method: 'POST' }),
  getXAssistantSettings: (projectId: string) => request<XAssistantSettings>(`/api/projects/${projectId}/x-assistant/settings`),
  updateXAssistantSettings: (projectId: string, settings: XAssistantSettingsInput) => request<XAssistantSettings>(`/api/projects/${projectId}/x-assistant/settings`, { method: 'PUT', body: JSON.stringify(settings) }),
  getXSources: (projectId: string) => request<XSourcePost[]>(`/api/projects/${projectId}/x-assistant/sources`),
  importXSource: (projectId: string, item: XSourcePostInput) => request<XSourcePost>(`/api/projects/${projectId}/x-assistant/sources`, { method: 'POST', body: JSON.stringify(item) }),
  dismissXSource: (projectId: string, sourceId: string) => request<XSourcePost>(`/api/projects/${projectId}/x-assistant/sources/${sourceId}/dismiss`, { method: 'POST' }),
  getXProposals: (projectId: string) => request<XReplyProposal[]>(`/api/projects/${projectId}/x-assistant/proposals`),
  transitionXProposal: (projectId: string, proposalId: string, targetStatus: string, selectedReply: string, publishedReplyUrl = '') => request<XReplyProposal>(`/api/projects/${projectId}/x-assistant/proposals/${proposalId}/transition`, { method: 'POST', body: JSON.stringify({ targetStatus, selectedReply, publishedReplyUrl }) }),
}
