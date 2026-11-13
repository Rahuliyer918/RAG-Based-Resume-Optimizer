'use client';

interface SkillBadgesProps {
  skills: string[];
  variant: 'aligned' | 'gap';
  emptyMessage?: string;
}

export default function SkillBadges({ skills, variant, emptyMessage }: SkillBadgesProps) {
  const styles = {
    aligned: {
      badge: 'bg-green-100 text-green-800 border border-green-200',
      icon: (
        <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M5 13l4 4L19 7" />
        </svg>
      ),
    },
    gap: {
      badge: 'bg-red-100 text-red-800 border border-red-200',
      icon: (
        <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
        </svg>
      ),
    },
  };

  const { badge, icon } = styles[variant];

  if (!skills || skills.length === 0) {
    return (
      <p className="text-sm text-slate-400 italic">
        {emptyMessage ?? (variant === 'aligned' ? 'No aligned skills found' : 'No gaps identified')}
      </p>
    );
  }

  return (
    <div className="flex flex-wrap gap-2">
      {skills.map((skill, index) => (
        <span key={index} className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${badge}`}>
          {icon}
          {skill}
        </span>
      ))}
    </div>
  );
}
