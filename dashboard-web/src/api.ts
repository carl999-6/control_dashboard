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
      message = errors ? Object.values(errors).flat()[0] : payload.title ?? message
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
  importMarketingEvents: (projectId: string, events: MarketingEventInput[]) => request<{ imported: number; totalCount: number }>(`/api/projects/${projectId}/marketing/events/import`, { method: 'POST', body: JSON.stringify({ events }) }),
}
