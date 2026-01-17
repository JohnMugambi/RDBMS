// app/page.tsx
"use client"

import { useState, useEffect } from "react"
import { type Task, tasksApi, ApiError } from "./lib/api"
import TaskCard from "./components/Taskcard"
import TaskForm from "./components/Taskform"
import FilterBar from "./components/FilterBar"
import StatsBar from "./components/StatsBar"
import { Plus, Database, AlertCircle, Loader2 } from "lucide-react"

export default function Home() {
  const [tasks, setTasks] = useState<Task[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [editingTask, setEditingTask] = useState<Task | undefined>(undefined)
  const [filter, setFilter] = useState<"all" | "active" | "completed">("all")
  const [priorityFilter, setPriorityFilter] = useState<"all" | "Low" | "Medium" | "High">("all")

  // Load tasks on mount
  useEffect(() => {
    loadTasks()
  }, [])

  const loadTasks = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await tasksApi.getAll()
      setTasks(data)
    } catch (err) {
      const errorMessage = err instanceof ApiError ? err.message : "Failed to load tasks"
      setError(errorMessage)
      console.error("Error loading tasks:", err)
    } finally {
      setLoading(false)
    }
  }

  const handleCreateTask = async (taskData: Omit<Task, "Id" | "CreatedAt">) => {
    try {
      const newTask = await tasksApi.create({
        ...taskData,
        CreatedAt: new Date().toISOString(),
      })
      setTasks([newTask, ...tasks])
      setShowForm(false)
    } catch (err) {
      const errorMessage = err instanceof ApiError ? err.message : "Failed to create task"
      setError(errorMessage)
      console.error("Error creating task:", err)
    }
  }

  const handleUpdateTask = async (taskData: Omit<Task, "Id" | "CreatedAt">) => {
    if (!editingTask) return

    try {
      await tasksApi.update(editingTask.Id, taskData)
      setTasks(tasks.map((t) => (t.Id === editingTask.Id ? { ...t, ...taskData } : t)))
      setEditingTask(undefined)
      setShowForm(false)
    } catch (err) {
      const errorMessage = err instanceof ApiError ? err.message : "Failed to update task"
      setError(errorMessage)
      console.error("Error updating task:", err)
    }
  }

  const handleToggleComplete = async (id: number) => {
    const task = tasks.find((t) => t.Id === id)
    if (!task) return

    try {
      await tasksApi.update(id, { Completed: !task.Completed })
      setTasks(tasks.map((t) => (t.Id === id ? { ...t, Completed: !t.Completed } : t)))
    } catch (err) {
      const errorMessage = err instanceof ApiError ? err.message : "Failed to toggle task"
      setError(errorMessage)
      console.error("Error toggling task:", err)
    }
  }

  const handleDeleteTask = async (id: number) => {
    if (!confirm("Are you sure you want to delete this task?")) return

    try {
      await tasksApi.delete(id)
      setTasks(tasks.filter((t) => t.Id !== id))
    } catch (err) {
      const errorMessage = err instanceof ApiError ? err.message : "Failed to delete task"
      setError(errorMessage)
      console.error("Error deleting task:", err)
    }
  }

  const handleEditTask = (task: Task) => {
    setEditingTask(task)
    setShowForm(true)
  }

  const handleCancelForm = () => {
    setShowForm(false)
    setEditingTask(undefined)
  }

  // Filter tasks
  const filteredTasks = tasks.filter((task) => {
    // Status filter
    if (filter === "active" && task.Completed) return false
    if (filter === "completed" && !task.Completed) return false

    // Priority filter
    if (priorityFilter !== "all" && task.Priority !== priorityFilter) return false
    return true
  })

  return (
    <div className="min-h-screen bg-gradient-to-b from-background via-background to-muted/20">
      <header className="bg-card border-b border-border/40 backdrop-blur-sm sticky top-0 z-40">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center">
                <Database className="w-6 h-6 text-primary" />
              </div>
              <div>
                <h1 className="text-2xl font-bold text-foreground">Task Manager</h1>
                <p className="text-xs text-muted-foreground mt-0.5">Organize your work efficiently</p>
              </div>
            </div>

            <button
              onClick={() => setShowForm(true)}
              className="flex items-center gap-2 bg-primary text-primary-foreground px-5 py-2.5 rounded-lg hover:bg-primary/90 active:scale-95 transition-all font-semibold shadow-lg hover:shadow-xl"
            >
              <Plus className="w-5 h-5" />
              <span>New Task</span>
            </button>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        {error && (
          <div className="mb-6 bg-destructive/5 border border-destructive/20 rounded-xl p-4 flex items-start gap-3 animate-slide-in">
            <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-destructive">Error</h3>
              <p className="text-sm text-destructive/80 mt-1">{error}</p>
            </div>
            <button
              onClick={() => setError(null)}
              className="text-destructive/60 hover:text-destructive transition-colors"
            >
              ×
            </button>
          </div>
        )}

        {/* Loading State */}
        {loading ? (
          <div className="flex flex-col items-center justify-center py-24">
            <Loader2 className="w-10 h-10 text-primary animate-spin mb-3" />
            <p className="text-muted-foreground">Loading your tasks...</p>
          </div>
        ) : (
          <>
            {/* Stats Bar */}
            <StatsBar tasks={tasks} />

            {/* Filter Bar */}
            <FilterBar
              filter={filter}
              onFilterChange={setFilter}
              priorityFilter={priorityFilter}
              onPriorityChange={setPriorityFilter}
            />

            {/* Tasks List */}
            {filteredTasks.length === 0 ? (
              <div className="text-center py-24">
                <div className="w-16 h-16 rounded-full bg-muted flex items-center justify-center mx-auto mb-4">
                  <Database className="w-8 h-8 text-muted-foreground" />
                </div>
                <h3 className="text-xl font-semibold text-foreground mb-2">
                  {tasks.length === 0 ? "No tasks yet" : "No tasks found"}
                </h3>
                <p className="text-muted-foreground mb-6">
                  {tasks.length === 0 ? "Create your first task to get started" : "Try adjusting your filters"}
                </p>
                {tasks.length === 0 && (
                  <button
                    onClick={() => setShowForm(true)}
                    className="inline-flex items-center gap-2 bg-primary text-primary-foreground px-6 py-3 rounded-lg hover:bg-primary/90 active:scale-95 transition-all font-semibold shadow-lg"
                  >
                    <Plus className="w-5 h-5" />
                    <span>Create Your First Task</span>
                  </button>
                )}
              </div>
            ) : (
              <div className="space-y-3">
                {filteredTasks.map((task) => (
                  <TaskCard
                    key={task.Id}
                    task={task}
                    onToggleComplete={handleToggleComplete}
                    onDelete={handleDeleteTask}
                    onEdit={handleEditTask}
                  />
                ))}
              </div>
            )}
          </>
        )}
      </main>

      {showForm && (
        <TaskForm
          task={editingTask}
          onSubmit={editingTask ? handleUpdateTask : handleCreateTask}
          onCancel={handleCancelForm}
        />
      )}

      <footer className="bg-card border-t border-border/40 mt-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <p className="text-center text-sm text-muted-foreground">
            Task Manager • Built with Next.js & Tailwind CSS
          </p>
        </div>
      </footer>
    </div>
  )
}
