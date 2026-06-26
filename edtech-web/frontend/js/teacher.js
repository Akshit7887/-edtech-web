async function loadStudents(page = 1, limit = 20) {
  try {
    const res = await api.get(`/api/teacher/students?page=${page}&limit=${limit}`);
    return res;
  } catch (e) {
    showAlert(e.message || 'Failed to load students');
    return { data: [], pagination: { page, limit, total: 0, total_pages: 0 } };
  }
}

async function loadStudentDetail(studentId) {
  try {
    const res = await api.get(`/api/teacher/students/${studentId}`);
    return res.data;
  } catch (e) {
    showAlert(e.message || 'Failed to load student details');
    return null;
  }
}

async function loadExams(page = 1, limit = 20) {
  try {
    const res = await api.get(`/api/exams?page=${page}&limit=${limit}`);
    return res;
  } catch (e) {
    showAlert(e.message || 'Failed to load exams');
    return { data: [], pagination: { page, limit, total: 0, total_pages: 0 } };
  }
}

async function createExam(data) {
  const res = await api.post('/api/exams', data);
  return res.data;
}

async function updateExam(id, data) {
  const res = await api.put(`/api/exams/${id}`, data);
  return res.data;
}

async function deleteExam(id) {
  const res = await api.del(`/api/exams/${id}`);
  return res;
}

async function activateExam(id) {
  const res = await api.post(`/api/exams/${id}/activate`);
  return res.data;
}

async function getExamDetail(id) {
  try {
    const res = await api.get(`/api/exams/${id}`);
    return res.data;
  } catch (e) {
    showAlert(e.message || 'Failed to load exam');
    return null;
  }
}

async function aiCreateExam(subject, topic, count, difficulty) {
  const res = await api.post('/api/exams/ai-create', { subject, topic, question_count: count, difficulty });
  return res;
}

async function getQuestionBank(examId) {
  try {
    const res = await api.get(`/api/teacher/questions/${examId}`);
    return res.data;
  } catch (e) {
    showAlert(e.message || 'Failed to load questions');
    return [];
  }
}

async function addQuestion(examId, data) {
  const res = await api.post(`/api/teacher/questions/${examId}`, data);
  return res.data;
}

async function updateQuestion(questionId, data) {
  const res = await api.put(`/api/teacher/questions/${questionId}`, data);
  return res.data;
}

async function deleteQuestion(questionId) {
  const res = await api.del(`/api/teacher/questions/${questionId}`);
  return res;
}

async function generateQuestions(examId, count, difficulty, syllabusText) {
  const res = await api.post('/api/questions/generate', { exam_id: examId, question_count: count, difficulty, syllabus_text: syllabusText });
  return res;
}

async function assignQuestions(examId) {
  const res = await api.post('/api/questions/assign', { exam_id: examId });
  return res;
}

async function publishQuestions(examId) {
  const res = await api.post(`/api/exams/${examId}/publish-questions`);
  return res.data;
}

async function bulkImportStudents(examId, csvText) {
  const res = await api.post(`/api/exams/${examId}/bulk-import`, { csv_text: csvText });
  return res.data;
}

async function getExamStatistics(examId) {
  try {
    const res = await api.get(`/api/exams/${examId}/statistics`);
    return res.data;
  } catch (e) {
    showAlert(e.message || 'Failed to load statistics');
    return null;
  }
}

async function getAttendanceReport(examId) {
  try {
    const res = await api.get(`/api/exams/${examId}/attendance`);
    return res.data;
  } catch (e) {
    showAlert(e.message || 'Failed to load attendance');
    return null;
  }
}

async function generateDeepLink(examId) {
  const res = await api.get(`/api/exams/${examId}/deep-link`);
  return res.data.deepLink;
}

async function loadClasses() {
  try {
    const res = await api.get('/api/teacher/classes');
    return res.data || [];
  } catch (e) {
    showAlert(e.message || 'Failed to load classes');
    return [];
  }
}

async function createClass(data) {
  const res = await api.post('/api/teacher/classes', data);
  return res.data;
}

async function addStudentsToClass(classId, studentIds) {
  const res = await api.post(`/api/teacher/classes/${classId}/students`, { student_ids: studentIds });
  return res;
}

async function removeStudentFromClass(classId, studentId) {
  const res = await api.del(`/api/teacher/classes/${classId}/students/${studentId}`);
  return res;
}

async function deleteClass(classId) {
  const res = await api.del(`/api/teacher/classes/${classId}`);
  return res;
}

async function scheduleExam(examId, scheduledAt) {
  const res = await api.put(`/api/teacher/schedule/${examId}`, { scheduled_at: scheduledAt });
  return res;
}

async function sendAnnouncement(title, message) {
  const res = await api.post('/api/teacher/announcement', { title, message });
  return res;
}

async function loadParentContacts() {
  try {
    const res = await api.get('/api/teacher/parent-contacts');
    return res.data || [];
  } catch (e) {
    showAlert(e.message || 'Failed to load parent contacts');
    return [];
  }
}

async function saveParentContact(studentId, data) {
  const res = await api.post(`/api/teacher/parent-contacts/${studentId}`, data);
  return res.data;
}

async function deleteParentContact(studentId) {
  const res = await api.del(`/api/teacher/parent-contacts/${studentId}`);
  return res;
}

async function sendParentReports(examId) {
  const res = await api.post(`/api/reports/send/${examId}`);
  return res;
}

async function getPendingReports(examId) {
  try {
    const res = await api.get(`/api/reports/pending/${examId}`);
    return res.data || [];
  } catch (e) {
    return [];
  }
}

async function exportPdf(examId) {
  try {
    await api.downloadPdf(`/api/exams/${examId}/export-pdf`);
  } catch (e) {
    showAlert(e.message || 'Export failed');
  }
}

async function generatePersonalizedQuestions(examId, count, difficulty) {
  const res = await api.post('/api/questions/generate-personalized', { exam_id: examId, question_count: count, difficulty });
  return res;
}

async function disqualifyStudent(sessionId, reason) {
  const res = await api.post(`/api/questions/disqualify/${sessionId}`, { reason });
  return res;
}

async function loadReportHistory(examId) {
  try {
    const res = await api.get(`/api/teacher/report-history/${examId}`);
    return res.data || [];
  } catch (e) {
    return [];
  }
}
