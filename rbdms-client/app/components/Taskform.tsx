"use client"

import type React from "react"

import type { Task } from "../lib/api"
import { X } from "lucide-react"
import { useState } from "react"

interface TaskFormProps {
  task?: Task
  onSubmit: (task: Omit<Task, "Id" | "CreatedAt">) => void
  onCancel: () => void
}

export default function TaskForm({ task, onSubmit, onCancel }: TaskFormProps) {
  const [formData, setFormData] = useState(() => ({
    title: task?.Title ?? "",
    description: task?.Description ?? "",
    completed: task?.Completed ?? false,
    priority: (task?.Priority as "Low" | "Medium" | "High") ?? "Medium",
  }))

   const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    // map local form fields to the Task shape expected by onSubmit
    onSubmit({
      Title: formData.title,
      Description: formData.description,
      Completed: formData.completed,
      Priority: formData.priority,
    })
  }

  return (
    <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-fade-in">
      <div className="bg-card rounded-2xl shadow-2xl max-w-md w-full p-8 animate-slide-in border border-border/40">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-foreground">{task ? "Edit Task" : "Create New Task"}</h2>
          <button
            onClick={onCancel}
            className="text-muted-foreground hover:text-foreground transition-colors p-1 hover:bg-muted rounded-lg"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div>
            <label htmlFor="title" className="block text-sm font-semibold text-foreground mb-2">
              Title <span className="text-destructive">*</span>
            </label>
            <input
              type="text"
              id="title"
              required
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              className="w-full px-4 py-2.5 border border-border rounded-lg focus:ring-2 focus:ring-primary/50 focus:border-primary bg-background transition-all"
              placeholder="Enter task title"
            />
          </div>

          <div>
            <label htmlFor="description" className="block text-sm font-semibold text-foreground mb-2">
              Description
            </label>
            <textarea
              id="description"
              rows={3}
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              className="w-full px-4 py-2.5 border border-border rounded-lg focus:ring-2 focus:ring-primary/50 focus:border-primary bg-background transition-all resize-none"
              placeholder="Enter task description (optional)"
            />
          </div>

          <div>
            <label htmlFor="priority" className="block text-sm font-semibold text-foreground mb-2">
              Priority
            </label>
            <select
              id="priority"
              value={formData.priority}
              onChange={(e) => setFormData({ ...formData, priority: e.target.value as "Low" | "Medium" | "High" })}
              className="w-full px-4 py-2.5 border border-border rounded-lg focus:ring-2 focus:ring-primary/50 focus:border-primary bg-background transition-all"
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>

          {task && (
            <div className="flex items-center gap-2 p-3 bg-muted/30 rounded-lg">
              <input
                type="checkbox"
                id="completed"
                checked={formData.completed}
                onChange={(e) => setFormData({ ...formData, completed: e.target.checked })}
                className="w-4 h-4 rounded border-border text-primary focus:ring-primary/50"
              />
              <label htmlFor="completed" className="text-sm font-medium text-foreground cursor-pointer">
                Mark as completed
              </label>
            </div>
          )}

          <div className="flex gap-3 pt-2">
            <button
              type="submit"
              className="flex-1 bg-primary text-primary-foreground px-4 py-2.5 rounded-lg hover:bg-primary/90 active:scale-95 transition-all font-semibold shadow-lg"
            >
              {task ? "Update Task" : "Create Task"}
            </button>
            <button
              type="button"
              onClick={onCancel}
              className="flex-1 bg-muted text-foreground px-4 py-2.5 rounded-lg hover:bg-muted hover:border-border transition-all font-semibold border border-border/20"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
