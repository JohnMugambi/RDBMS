"use client"

import { Filter } from "lucide-react"

interface FilterBarProps {
  filter: "all" | "active" | "completed"
  onFilterChange: (filter: "all" | "active" | "completed") => void
  priorityFilter: "all" | "Low" | "Medium" | "High"
  onPriorityChange: (priority: "all" | "Low" | "Medium" | "High") => void
}

export default function FilterBar({ filter, onFilterChange, priorityFilter, onPriorityChange }: FilterBarProps) {
  return (
    <div className="bg-card rounded-xl border border-border/40 p-6 mb-8">
      <div className="flex items-center gap-2 mb-5">
        <Filter className="w-5 h-5 text-primary" />
        <h3 className="text-sm font-semibold text-foreground">Filters</h3>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <label className="block text-xs font-semibold text-foreground mb-3 uppercase tracking-wide">Status</label>
          <div className="flex gap-2">
            {(["all", "active", "completed"] as const).map((status) => (
              <button
                key={status}
                onClick={() => onFilterChange(status)}
                className={`flex-1 px-4 py-2.5 rounded-lg text-sm font-medium transition-all ${
                  filter === status
                    ? "bg-primary text-primary-foreground shadow-md"
                    : "bg-muted text-foreground hover:bg-muted/80"
                }`}
              >
                {status.charAt(0).toUpperCase() + status.slice(1)}
              </button>
            ))}
          </div>
        </div>

        <div>
          <label className="block text-xs font-semibold text-foreground mb-3 uppercase tracking-wide">Priority</label>
          <select
            value={priorityFilter}
            onChange={(e) => onPriorityChange(e.target.value as "all" | "Low" | "Medium" | "High")}
            className="w-full px-4 py-2.5 border border-border rounded-lg focus:ring-2 focus:ring-primary/50 focus:border-primary bg-background transition-all text-sm font-medium"
          >
            <option value="all">All Priorities</option>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
          </select>
        </div>
      </div>
    </div>
  )
}
