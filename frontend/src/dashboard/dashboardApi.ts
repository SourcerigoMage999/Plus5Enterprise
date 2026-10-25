import { useEffect, useState } from 'react'
import { getJson } from '../api/apiClient.ts'

export interface DashboardSession {
  readonly id: string
  readonly deliveryMode: 1 | 2
  readonly groupId: string | null
  readonly studentId: string | null
  readonly contextName: string
  readonly schoolGradeName: string
  readonly programId: string | null
  readonly programName: string | null
  readonly title: string | null
  readonly startsAtUtc: string
  readonly endsAtUtc: string
  readonly timeZoneId: string
  readonly locationId: string | null
  readonly locationName: string | null
  readonly online: boolean
  readonly status: 1 | 2 | 3
  readonly participantCount: number
}

export interface DashboardStudentOverview {
  readonly totalCount: number
  readonly activeCount: number
  readonly onHoldCount: number
  readonly inactiveCount: number
}

export interface DashboardData {
  readonly timeZoneId: string
  readonly today: string
  readonly generatedAtUtc: string
  readonly todaySessions: DashboardSession[]
  readonly nextSession: DashboardSession | null
  readonly students: DashboardStudentOverview
}

interface DashboardState {
  readonly revision: number
  readonly data?: DashboardData
  readonly error?: string
}

export function useDashboard(revision: number) {
  const [state, setState] = useState<DashboardState>()

  useEffect(() => {
    const controller = new AbortController()
    getJson<DashboardData>('/dashboard', controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) setState({ revision, data })
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setState({
            revision,
            error: error instanceof Error
              ? error.message
              : 'Radni stol trenutačno nije moguće učitati.',
          })
        }
      })

    return () => controller.abort()
  }, [revision])

  return state?.revision === revision ? state : undefined
}
