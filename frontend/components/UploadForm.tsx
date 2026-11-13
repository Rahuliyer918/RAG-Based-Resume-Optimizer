'use client';

import { useState, useRef, DragEvent, ChangeEvent } from 'react';
import { useRouter } from 'next/navigation';
import { submitAnalysis } from '@/lib/api';
import { AnalysisStyle } from '@/lib/types';
import JobDescriptionInput from './JobDescriptionInput';

type InputMode = 'upload' | 'paste';

const STYLE_OPTIONS: { value: AnalysisStyle; label: string; description: string }[] = [
  {
    value: 'metrics-focused',
    label: 'Metrics Focused',
    description: 'Emphasizes quantifiable achievements and data-driven impact',
  },
  {
    value: 'narrative',
    label: 'Narrative',
    description: 'Tells a compelling story of your career progression',
  },
  {
    value: 'concise',
    label: 'Concise',
    description: 'Short, punchy bullets optimized for ATS scanning',
  },
];

export default function UploadForm() {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [inputMode, setInputMode] = useState<InputMode>('upload');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [resumeText, setResumeText] = useState('');
  const [isDragging, setIsDragging] = useState(false);
  const [jobDescriptions, setJobDescriptions] = useState<string[]>(['']);
  const [style, setStyle] = useState<AnalysisStyle>('metrics-focused');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleFileSelect = (file: File) => {
    if (file.type !== 'application/pdf') {
      setValidationError('Please upload a PDF file.');
      return;
    }
    setSelectedFile(file);
    setValidationError(null);
  };

  const handleFileInputChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFileSelect(file);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files?.[0];
    if (file) handleFileSelect(file);
  };

  const handleJobDescriptionChange = (index: number, value: string) => {
    setJobDescriptions((prev) => {
      const updated = [...prev];
      updated[index] = value;
      return updated;
    });
  };

  const handleAddJobDescription = () => {
    if (jobDescriptions.length < 5) {
      setJobDescriptions((prev) => [...prev, '']);
    }
  };

  const handleRemoveJobDescription = (index: number) => {
    setJobDescriptions((prev) => prev.filter((_, i) => i !== index));
  };

  const validate = (): boolean => {
    if (inputMode === 'upload' && !selectedFile) {
      setValidationError('Please upload a PDF resume.');
      return false;
    }
    if (inputMode === 'paste' && !resumeText.trim()) {
      setValidationError('Please paste your resume text.');
      return false;
    }
    const nonEmptyJDs = jobDescriptions.filter((jd) => jd.trim().length > 0);
    if (nonEmptyJDs.length === 0) {
      setValidationError('Please add at least one job description.');
      return false;
    }
    setValidationError(null);
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      const formData = new FormData();
      if (inputMode === 'upload' && selectedFile) {
        formData.append('resume', selectedFile);
      } else if (inputMode === 'paste') {
        formData.append('resumeText', resumeText);
      }

      const nonEmptyJDs = jobDescriptions.filter((jd) => jd.trim().length > 0);
      nonEmptyJDs.forEach((jd) => {
        formData.append('jobDescriptions', jd);
      });
      formData.append('style', style);

      const { analysisId } = await submitAnalysis(formData);
      router.push(`/results?id=${analysisId}`);
    } catch (err) {
      setValidationError(err instanceof Error ? err.message : 'Failed to submit. Please try again.');
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="bg-white rounded-2xl shadow-lg border border-slate-100 overflow-hidden">
        {/* Header */}
        <div className="px-6 py-5 border-b border-slate-100">
          <h2 className="text-lg font-semibold text-slate-900">Upload Your Resume</h2>
          <p className="text-sm text-slate-500 mt-0.5">Start by providing your resume and the job descriptions you want to target</p>
        </div>

        <div className="px-6 py-6 space-y-8">
          {/* Resume Input Toggle */}
          <div>
            <div className="flex gap-1 p-1 bg-slate-100 rounded-lg w-fit mb-4">
              <button
                type="button"
                onClick={() => {
                  setInputMode('upload');
                  setValidationError(null);
                }}
                className={`px-4 py-2 text-sm font-medium rounded-md transition-all ${
                  inputMode === 'upload'
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Upload PDF
              </button>
              <button
                type="button"
                onClick={() => {
                  setInputMode('paste');
                  setValidationError(null);
                }}
                className={`px-4 py-2 text-sm font-medium rounded-md transition-all ${
                  inputMode === 'paste'
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Paste Text
              </button>
            </div>

            {inputMode === 'upload' ? (
              <div>
                <div
                  onDragOver={handleDragOver}
                  onDragLeave={handleDragLeave}
                  onDrop={handleDrop}
                  onClick={() => fileInputRef.current?.click()}
                  className={`border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all ${
                    isDragging
                      ? 'border-indigo-500 bg-indigo-50'
                      : selectedFile
                      ? 'border-green-400 bg-green-50'
                      : 'border-slate-200 hover:border-indigo-300 hover:bg-slate-50'
                  }`}
                >
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".pdf"
                    className="hidden"
                    onChange={handleFileInputChange}
                  />
                  {selectedFile ? (
                    <div className="flex flex-col items-center gap-2">
                      <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center">
                        <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                      </div>
                      <p className="font-medium text-slate-800">{selectedFile.name}</p>
                      <p className="text-xs text-slate-500">{(selectedFile.size / 1024).toFixed(1)} KB · Click to change</p>
                    </div>
                  ) : (
                    <div className="flex flex-col items-center gap-3">
                      <div className="w-12 h-12 bg-slate-100 rounded-full flex items-center justify-center">
                        <svg className="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                        </svg>
                      </div>
                      <div>
                        <p className="font-medium text-slate-700">
                          {isDragging ? 'Drop your PDF here' : 'Drag & drop your PDF'}
                        </p>
                        <p className="text-sm text-slate-500 mt-0.5">or click to browse files</p>
                      </div>
                      <span className="text-xs text-slate-400 bg-slate-100 px-2 py-1 rounded">PDF only</span>
                    </div>
                  )}
                </div>
              </div>
            ) : (
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  Paste your resume text
                </label>
                <textarea
                  value={resumeText}
                  onChange={(e) => setResumeText(e.target.value)}
                  placeholder="Paste your complete resume here..."
                  rows={10}
                  className="w-full px-3 py-2.5 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-y text-slate-800 placeholder-slate-400 bg-white"
                />
              </div>
            )}
          </div>

          {/* Job Descriptions */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <div>
                <h3 className="text-sm font-semibold text-slate-900">Job Descriptions</h3>
                <p className="text-xs text-slate-500 mt-0.5">Add up to 5 job descriptions to analyze simultaneously</p>
              </div>
              <span className="text-xs text-slate-400 bg-slate-100 px-2 py-1 rounded-full">
                {jobDescriptions.length}/5
              </span>
            </div>
            <div className="space-y-4">
              {jobDescriptions.map((jd, index) => (
                <JobDescriptionInput
                  key={index}
                  index={index}
                  value={jd}
                  onChange={handleJobDescriptionChange}
                  onRemove={handleRemoveJobDescription}
                  canRemove={jobDescriptions.length > 1}
                />
              ))}
            </div>
            {jobDescriptions.length < 5 && (
              <button
                type="button"
                onClick={handleAddJobDescription}
                className="mt-3 flex items-center gap-1.5 text-sm text-indigo-600 hover:text-indigo-700 font-medium transition-colors"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                Add Job Description
              </button>
            )}
          </div>

          {/* Style Selector */}
          <div>
            <h3 className="text-sm font-semibold text-slate-900 mb-1">Writing Style</h3>
            <p className="text-xs text-slate-500 mb-3">Choose how your tailored resume bullets should be written</p>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              {STYLE_OPTIONS.map((option) => (
                <label
                  key={option.value}
                  className={`relative flex flex-col gap-1 p-4 rounded-xl border-2 cursor-pointer transition-all ${
                    style === option.value
                      ? 'border-indigo-500 bg-indigo-50'
                      : 'border-slate-200 hover:border-slate-300 bg-white'
                  }`}
                >
                  <input
                    type="radio"
                    name="style"
                    value={option.value}
                    checked={style === option.value}
                    onChange={() => setStyle(option.value)}
                    className="sr-only"
                  />
                  <div className="flex items-center gap-2">
                    <div
                      className={`w-4 h-4 rounded-full border-2 flex-shrink-0 transition-all ${
                        style === option.value
                          ? 'border-indigo-600 bg-indigo-600'
                          : 'border-slate-300'
                      }`}
                    >
                      {style === option.value && (
                        <div className="w-full h-full rounded-full bg-white scale-50 transform" />
                      )}
                    </div>
                    <span className={`text-sm font-medium ${style === option.value ? 'text-indigo-700' : 'text-slate-700'}`}>
                      {option.label}
                    </span>
                  </div>
                  <p className="text-xs text-slate-500 leading-relaxed pl-6">{option.description}</p>
                </label>
              ))}
            </div>
          </div>

          {/* Validation Error */}
          {validationError && (
            <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-lg">
              <svg className="w-4 h-4 text-red-500 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p className="text-sm text-red-700">{validationError}</p>
            </div>
          )}

          {/* Submit Button */}
          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 text-white font-semibold py-3.5 px-6 rounded-xl transition-all text-sm shadow-sm hover:shadow-md disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {isSubmitting ? (
              <>
                <svg className="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Submitting...
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
                Analyze Resume
              </>
            )}
          </button>
        </div>
      </div>
    </form>
  );
}
