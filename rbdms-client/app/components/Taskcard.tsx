"use client"

import type { Task } from "../lib/api"
import { CheckCircle2, Circle, Trash2, Edit, Clock } from "lucide-react"
import { format } from "date-fns"

interface TaskCardProps {
  task: Task
  onToggleComplete: (id: number) => void
  onDelete: (id: number) => void
  onEdit: (task: Task) => void
}

const priorityColors = {
  Low: "bg-blue-100 text-blue-800",
  Medium: "bg-yellow-100 text-yellow-800",
  High: "bg-red-100 text-red-800",
}

export default function TaskCard({ task, onToggleComplete, onDelete, onEdit }: TaskCardProps) {
  return (
    <div className="bg-card rounded-xl border border-border/40 p-5 hover:border-primary/20 hover:shadow-md transition-all duration-200 animate-slide-in group">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3 flex-1 min-w-0">
          <button
            onClick={() => onToggleComplete(task.Id)}
            className="mt-1 flex-shrink-0 focus:outline-none focus:ring-2 focus:ring-primary/50 rounded-full p-1 transition-colors"
          >
            {task.Completed ? (
              <CheckCircle2 className="w-6 h-6 text-accent" />
            ) : (
              <Circle className="w-6 h-6 text-muted-foreground hover:text-primary transition-colors" />
            )}
          </button>

          <div className="flex-1 min-w-0">
            <h3
              className={`text-base font-semibold transition-all ${
                task.Completed ? "line-through text-muted-foreground" : "text-foreground"
              }`}
            >
              {task.Title}
            </h3>

            {task.Description && (
              <p className="text-sm text-muted-foreground mt-1.5 line-clamp-2">{task.Description}</p>
            )}

            <div className="flex items-center gap-2 mt-3">
              {task.Priority && (
                <span
                  className={`px-2.5 py-1 rounded-full text-xs font-medium transition-colors ${
                    task.Priority === "High"
                      ? "bg-destructive/10 text-destructive"
                      : task.Priority === "Medium"
                        ? "bg-accent/10 text-accent"
                        : "bg-muted text-muted-foreground"
                  }`}
                >
                  {task.Priority}
                </span>
              )}

              <div className="flex items-center text-xs text-muted-foreground">
                <Clock className="w-3 h-3 mr-1.5" />
                {format(new Date(task.CreatedAt), "MMM d, yyyy")}
              </div>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button
            onClick={() => onEdit(task)}
            className="p-2 text-muted-foreground hover:text-primary hover:bg-primary/5 rounded-lg transition-all"
            title="Edit task"
          >
            <Edit className="w-4 h-4" />
          </button>

          <button
            onClick={() => onDelete(task.Id)}
            className="p-2 text-muted-foreground hover:text-destructive hover:bg-destructive/5 rounded-lg transition-all"
            title="Delete task"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  )
}
