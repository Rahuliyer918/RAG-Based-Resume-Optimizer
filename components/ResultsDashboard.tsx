'use client';

import { useState } from 'react';
import Link from 'next/link';
import { AnalyzeResponse, JobAnalysisResult } from '@/lib/types';
import MatchScoreGauge from './MatchScoreGauge';
import SkillBadges from './SkillBadges';
import UnderrepresentedSkills from './UnderrepresentedSkills';
import TailoredBullets from './TailoredBullets';

interface ResultsDashboardProps {
  data: AnalyzeResponse;
}

function JobResultPanel({ result }: { result: JobAnalysisResult }) {
  return (
    <div className="space-y-6">
      {/* Score + Summary Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Match Score Card */}
        <div className="bg-slate-900 rounded-2xl p-6 flex flex-col items-center justify-center gap-3 text-center">
          <h3 className="text-slate-400 text-sm font-medium uppercase tracking-wider">Match Score</h3>
          <MatchScoreGauge score={result.matchScore} />
        </div>

        {/* Aligned Skills */}
        <div className="bg-white rounded-2xl p-6 border border-slate-100 shadow-sm">
          <div className="flex items-center gap-2 mb-3">
            <div className="w-7 h-7 bg-green-100 rounded-lg flex items-center justify-center">
              <svg className="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <h3 className="text-sm font-semibold text-slate-700">Aligned Skills</h3>
          </div>
          <SkillBadges skills={result.alignedSkills} variant="aligned" />
        </div>

        {/* True Gaps */}
        <div className="bg-white rounded-2xl p-6 border border-slate-100 shadow-sm">
          <div className="flex items-center gap-2 mb-3">
            <div className="w-7 h-7 bg-red-100 rounded-lg flex items-center justify-center">
              <svg className="w-4 h-4 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
            <h3 className="text-sm font-semibold text-slate-700">True Gaps</h3>
          </div>
          <SkillBadges skills={result.trueGaps} variant="gap" />
        </div>
      </div>

      {/* Underrepresented Skills */}
      <div className="bg-white rounded-2xl p-6 border border-slate-100 shadow-sm">
        <div className="flex items-center gap-2 mb-4">
          <div className="w-7 h-7 bg-amber-100 rounded-lg flex items-center justify-center">
            <svg className="w-4 h-4 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div>
            <h3 className="text-sm font-semibold text-slate-700">Underrepresented Skills</h3>
            <p className="text-xs text-slate-500">Skills you have but aren&apos;t highlighting well enough</p>
          </div>
        </div>
        <UnderrepresentedSkills skills={result.underrepresentedSkills} />
      </div>

      {/* Tailored Resume Section */}
      <div className="bg-white rounded-2xl p-6 border border-slate-100 shadow-sm">
        <div className="flex items-center gap-2 mb-5">
          <div className="w-7 h-7 bg-indigo-100 rounded-lg flex items-center justify-center">
            <svg className="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <div>
            <h3 className="text-sm font-semibold text-slate-700">Tailored Resume</h3>
            <p className="text-xs text-slate-500">AI-optimized content for this specific job</p>
          </div>
        </div>

        {/* Summary */}
        {result.tailoredResume?.summary && (
          <div className="mb-6">
            <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-3">Professional Summary</h4>
            <div className="p-4 bg-indigo-50 rounded-xl border border-indigo-100">
              <p className="text-sm text-slate-800 leading-relaxed">{result.tailoredResume.summary}</p>
            </div>
          </div>
        )}

        {/* Experience Bullets */}
        {result.tailoredResume?.experienceBullets && result.tailoredResume.experienceBullets.length > 0 && (
          <div className="mb-6">
            <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-3">Experience Bullets</h4>
            <TailoredBullets bullets={result.tailoredResume.experienceBullets} />
          </div>
        )}

        {/* Skills Section */}
        {result.tailoredResume?.skillsSection && (
          <div>
            <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-3">Skills Section</h4>
            <div className="p-4 bg-slate-50 rounded-xl border border-slate-200">
              <p className="text-sm text-slate-700 whitespace-pre-wrap leading-relaxed">{result.tailoredResume.skillsSection}</p>
            </div>
          </div>
        )}

        {!result.tailoredResume?.summary && (!result.tailoredResume?.experienceBullets || result.tailoredResume.experienceBullets.length === 0) && !result.tailoredResume?.skillsSection && (
          <p className="text-sm text-slate-400 italic">No tailored resume content available</p>
        )}
      </div>
    </div>
  );
}

export default function ResultsDashboard({ data }: ResultsDashboardProps) {
  const [activeTab, setActiveTab] = useState(0);

  const results = data.results ?? [];
  const durationSeconds = data.processingDurationMs
    ? (data.processingDurationMs / 1000).toFixed(1)
    : null;

  return (
    <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Back link */}
      <Link
        href="/"
        className="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 transition-colors mb-6 group"
      >
        <svg
          className="w-4 h-4 group-hover:-translate-x-0.5 transition-transform"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
        </svg>
        Back to home
      </Link>

      {/* Page title */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">Analysis Results</h1>
        <p className="text-slate-500 text-sm mt-1">
          {results.length} job description{results.length !== 1 ? 's' : ''} analyzed
        </p>
      </div>

      {/* Tabs (only if multiple results) */}
      {results.length > 1 && (
        <div className="flex gap-1 p-1 bg-slate-100 rounded-xl w-fit mb-6 flex-wrap">
          {results.map((result, index) => (
            <button
              key={index}
              type="button"
              onClick={() => setActiveTab(index)}
              className={`px-4 py-2 text-sm font-medium rounded-lg transition-all ${
                activeTab === index
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              Job {index + 1}
            </button>
          ))}
        </div>
      )}

      {/* Result Panel */}
      {results.length > 0 ? (
        <JobResultPanel result={results[activeTab] ?? results[0]} />
      ) : (
        <div className="text-center py-16 text-slate-400">
          <p>No results available.</p>
        </div>
      )}

      {/* Processing duration */}
      {durationSeconds && (
        <p className="text-center text-xs text-slate-400 mt-8">
          Analyzed in {durationSeconds} seconds
        </p>
      )}
    </div>
  );
}
