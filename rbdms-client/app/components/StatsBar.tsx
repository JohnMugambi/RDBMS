"use client"

import type { Task } from "../lib/api"
import { CheckCircle2, Circle, ListTodo, TrendingUp } from "lucide-react"

interface StatsBarProps {
  tasks: Task[]
}

export default function StatsBar({ tasks }: StatsBarProps) {
  const total = tasks.length
  const completed = tasks.filter((t) => t.Completed).length
  const active = total - completed
  const completionRate = total > 0 ? Math.round((completed / total) * 100) : 0

  const stats = [
    {
      label: "Total Tasks",
      value: total,
      icon: ListTodo,
      color: "text-blue-600",
      bgColor: "bg-blue-50",
    },
    {
      label: "Active",
      value: active,
      icon: Circle,
      color: "text-yellow-600",
      bgColor: "bg-yellow-50",
    },
    {
      label: "Completed",
      value: completed,
      icon: CheckCircle2,
      color: "text-green-600",
      bgColor: "bg-green-50",
    },
    {
      label: "Completion Rate",
      value: `${completionRate}%`,
      icon: TrendingUp,
      color: "text-purple-600",
      bgColor: "bg-purple-50",
    },
  ]

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
      {stats.map((stat) => {
        const Icon = stat.icon
        return (
          <div
            key={stat.label}
            className="bg-card rounded-xl border border-border/40 p-5 hover:border-primary/20 hover:shadow-md transition-all group"
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-2">{stat.label}</p>
                <p className={`text-3xl font-bold tracking-tight`}>
                  <span className={stat.color}>{stat.value}</span>
                </p>
              </div>
              <div
                className={`w-12 h-12 rounded-lg flex items-center justify-center transition-all group-hover:scale-110 ${stat.bgColor}`}
              >
                <Icon className={`w-6 h-6 ${stat.color}`} />
              </div>
            </div>
          </div>
        )
      })}
    </div>
  )
}
