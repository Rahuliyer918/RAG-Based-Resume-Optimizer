'use client';

interface JobDescriptionInputProps {
  index: number;
  value: string;
  onChange: (index: number, value: string) => void;
  onRemove: (index: number) => void;
  canRemove: boolean;
}

export default function JobDescriptionInput({
  index,
  value,
  onChange,
  onRemove,
  canRemove,
}: JobDescriptionInputProps) {
  return (
    <div className="relative group">
      <div className="flex items-center justify-between mb-1.5">
        <label className="text-sm font-medium text-slate-700">
          Job Description {index + 1}
        </label>
        {canRemove && (
          <button
            type="button"
            onClick={() => onRemove(index)}
            className="text-slate-400 hover:text-red-500 transition-colors p-1 rounded hover:bg-red-50"
            aria-label={`Remove job description ${index + 1}`}
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>
      <textarea
        value={value}
        onChange={(e) => onChange(index, e.target.value)}
        placeholder="Paste the full job description here..."
        rows={5}
        className="w-full px-3 py-2.5 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-y text-slate-800 placeholder-slate-400 bg-white transition-shadow"
        required
      />
    </div>
  );
}
