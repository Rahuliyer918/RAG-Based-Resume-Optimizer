'use client';

import { useState } from 'react';
import { UnderrepresentedSkill } from '@/lib/types';

interface UnderrepresentedSkillsProps {
  skills: UnderrepresentedSkill[];
}

function SkillCard({ skill }: { skill: UnderrepresentedSkill }) {
  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <div className="border border-slate-200 rounded-lg overflow-hidden">
      <button
        type="button"
        onClick={() => setIsExpanded((prev) => !prev)}
        className="w-full flex items-center justify-between px-4 py-3 bg-amber-50 hover:bg-amber-100 transition-colors text-left"
      >
        <div className="flex items-center gap-2">
          <span className="w-2 h-2 rounded-full bg-amber-500 flex-shrink-0" />
          <span className="text-sm font-medium text-slate-800">{skill.skill}</span>
        </div>
        <svg
          className={`w-4 h-4 text-slate-500 flex-shrink-0 transition-transform ${isExpanded ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      {isExpanded && (
        <div className="px-4 py-3 bg-white space-y-3">
          {skill.currentText && (
            <div>
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">Current Text</p>
              <p className="text-sm text-slate-700 bg-slate-50 rounded-md p-2.5 border border-slate-100 italic">
                &ldquo;{skill.currentText}&rdquo;
              </p>
            </div>
          )}
          <div>
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">Why It Matters</p>
            <p className="text-sm text-slate-700">{skill.reason}</p>
          </div>
        </div>
      )}
    </div>
  );
}

export default function UnderrepresentedSkills({ skills }: UnderrepresentedSkillsProps) {
  if (!skills || skills.length === 0) {
    return (
      <p className="text-sm text-slate-400 italic">No underrepresented skills identified</p>
    );
  }

  return (
    <div className="space-y-2">
      {skills.map((skill, index) => (
        <SkillCard key={index} skill={skill} />
      ))}
    </div>
  );
}
