class RealtimeClient {
  constructor() {
    this.connections = {};
    this.handlers = {};
    this.isConnected = {};
  }

  getBaseUrl() {
    return API_BASE;
  }

  _getToken() {
    return localStorage.getItem('token') || '';
  }

  _buildHubConnection(url) {
    const token = this._getToken();
    return new signalR.HubConnectionBuilder()
      .withUrl(url, { accessTokenFactory: () => token })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }

  async connectDashboard(role, userId) {
    const key = 'dashboard';
    if (this.connections[key]) return;
    const conn = this._buildHubConnection(this.getBaseUrl() + '/hubs/dashboard');

    conn.onreconnecting(() => console.log('[Realtime] Dashboard reconnecting...'));
    conn.onreconnected(() => {
      console.log('[Realtime] Dashboard reconnected');
      if (role === 'teacher') conn.invoke('JoinTeacherGroup', userId);
      else conn.invoke('JoinStudentGroup', userId);
    });

    this.connections[key] = conn;
    try {
      await conn.start();
      if (role === 'teacher') await conn.invoke('JoinTeacherGroup', userId);
      else await conn.invoke('JoinStudentGroup', userId);
      this.isConnected[key] = true;
    } catch (e) {
      console.warn('[Realtime] Dashboard connection failed:', e.message);
      this.isConnected[key] = false;
    }
    return conn;
  }

  async connectExam(examId) {
    const key = 'exam_' + examId;
    if (this.connections[key]) return;
    const conn = this._buildHubConnection(this.getBaseUrl() + '/hubs/exam');

    conn.onreconnecting(() => console.log('[Realtime] Exam reconnecting...'));
    conn.onreconnected(() => {
      console.log('[Realtime] Exam reconnected');
      conn.invoke('JoinExamGroup', examId);
    });

    this.connections[key] = conn;
    try {
      await conn.start();
      await conn.invoke('JoinExamGroup', examId);
      this.isConnected[key] = true;
    } catch (e) {
      console.warn('[Realtime] Exam connection failed:', e.message);
      this.isConnected[key] = false;
    }
    return conn;
  }

  async connectNotifications(userId) {
    const key = 'notifications';
    if (this.connections[key]) return;
    const conn = this._buildHubConnection(this.getBaseUrl() + '/hubs/notification');

    conn.onreconnecting(() => console.log('[Realtime] Notifications reconnecting...'));
    conn.onreconnected(() => {
      console.log('[Realtime] Notifications reconnected');
      conn.invoke('JoinUserGroup', userId);
    });

    this.connections[key] = conn;
    try {
      await conn.start();
      await conn.invoke('JoinUserGroup', userId);
      this.isConnected[key] = true;
    } catch (e) {
      console.warn('[Realtime] Notifications connection failed:', e.message);
      this.isConnected[key] = false;
    }
    return conn;
  }

  on(event, callback) {
    this.handlers[event] = callback;
    Object.values(this.connections).forEach(conn => {
      if (conn) conn.off(event);
    });
    Object.values(this.connections).forEach(conn => {
      if (conn) conn.on(event, (...args) => {
        if (this.handlers[event]) this.handlers[event](...args);
      });
    });
  }

  disconnectAll() {
    Object.entries(this.connections).forEach(([key, conn]) => {
      if (conn) conn.stop();
    });
    this.connections = {};
    this.isConnected = {};
  }
}

function getRealtimeBaseUrl() {
  const base = API_BASE;
  return base;
}

const realtime = new RealtimeClient();
