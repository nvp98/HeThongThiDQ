/**
 * k6 Load Test — Mô phỏng luồng thi hoàn chỉnh
 *
 * Chạy:
 *   cd k6
 *   k6 run exam-load-test.js
 *
 * Debug (1 user, thấy log chi tiết):
 *   k6 run --vus 1 --iterations 1 exam-load-test.js
 */

import http from "k6/http";
import { sleep, check, group } from "k6";
import { SharedArray } from "k6/data";
import { Trend, Rate, Counter } from "k6/metrics";

// ── Load danh sách user ───────────────────────────────────────────────────────
const users = new SharedArray("users", function () {
  return JSON.parse(open("./users.json"));
});

const BASE_URL = "https://qday.hoaphatdungquat.vn";

// ── Custom metrics ────────────────────────────────────────────────────────────
const mLogin = new Trend("dur_login_ms", true);
const mLoad = new Trend("dur_exam_load_ms", true);
const mAutosave = new Trend("dur_autosave_ms", true);
const mSubmit = new Trend("dur_submit_ms", true);
const rSuccess = new Rate("exam_success_rate");
const cSubmit = new Counter("exam_submit_total");
const cSuccess = new Counter("exam_submit_success");

// ── Cấu hình test ────────────────────────────────────────────────────────────
const VUS = 600; // ← số VU đồng thời
const MAX_SUBMITS = 17000; // ← MODE 2: tổng số lần thi tối đa rồi dừng
const DURATION = "30m"; // ← MODE 2: timeout cứng nếu 10000 submit chưa xong

// Chế độ chạy:
// MODE 1: Mỗi VU thi 1 lần → đo max concurrent user chịu được
// MODE 2: Tổng MAX_SUBMITS lần thi, 2000 VU chạy song song → đo throughput

const MODE = 2; // 1 = single-shot | 2 = shared-iterations

export const options =
  MODE === 1
    ? {
        scenarios: {
          single: {
            executor: "per-vu-iterations",
            vus: VUS,
            iterations: 1,
            maxDuration: "15m",
          },
        },
        thresholds: {
          http_req_duration: ["p(95)<5000"],
          http_req_failed: ["rate<0.10"],
          exam_success_rate: ["rate>0.85"],
          dur_login_ms: ["p(95)<3000"],
          dur_submit_ms: ["p(95)<6000"],
        },
      }
    : {
        scenarios: {
          load: {
            executor: "shared-iterations",
            vus: VUS,
            iterations: MAX_SUBMITS,
            maxDuration: DURATION,
          },
        },
        thresholds: {
          http_req_duration: ["p(95)<5000"],
          http_req_failed: ["rate<0.10"],
          exam_success_rate: ["rate>0.85"],
          dur_login_ms: ["p(95)<3000"],
          dur_submit_ms: ["p(95)<6000"],
        },
      };

// ── Helpers ───────────────────────────────────────────────────────────────────
function parseCsrf(html) {
  const m = html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
  return m ? m[1] : "";
}

function parseExamSession(html) {
  const marker = "var examSession =";
  const start = html.indexOf(marker);
  if (start === -1) return null;

  // Nếu là null (không có session): var examSession = null;
  const after = html.substring(start + marker.length).trimStart();
  if (after.startsWith("null")) return null;

  const jStart = html.indexOf("{", start);
  if (jStart === -1) return null;

  let depth = 0,
    i = jStart;
  for (; i < html.length; i++) {
    if (html[i] === "{") depth++;
    else if (html[i] === "}") {
      depth--;
      if (depth === 0) break;
    }
  }
  try {
    return JSON.parse(html.substring(jStart, i + 1));
  } catch (e) {
    console.error(`parseExamSession JSON.parse lỗi: ${e}`);
    return null;
  }
}

function parseQuestionIds(html) {
  const ids = [];
  const re = /name="\[(\d+)\]\.IDCH"\s+value="(\d+)"/g;
  let m;
  while ((m = re.exec(html)) !== null) ids.push(parseInt(m[2]));
  return ids;
}

// ── Flow chính ────────────────────────────────────────────────────────────────
export default function () {
  // Rải VU để tránh thundering herd: MODE 1 rải 60s, MODE 2 rải 5s
  sleep(MODE === 1 ? Math.random() * 60 : Math.random() * 5);

  // Mỗi iteration dùng user khác nhau → tránh bị chặn duplicate submit (SET NX)
  // VU1 iter0 → users[0], VU1 iter1 → users[VUS], VU2 iter0 → users[1], ...
  const userIndex = (__VU - 1 + __ITER * VUS) % users.length;
  const user = users[userIndex];
  const lhid = user.lhid || 1;
  let loggedIn = false;
  let examSession = null;
  let questionIds = [];

  // ── 1. Login ──────────────────────────────────────────────────────────────
  group("1_login", function () {
    // Lấy trang login để parse CSRF token (nếu app bật antiforgery)
    const loginPage = http.get(`${BASE_URL}/Login`, {
      tags: { step: "login_page" },
    });
    const csrf = parseCsrf(loginPage.body || "");

    const t0 = Date.now();
    const res = http.post(
      `${BASE_URL}/Login/LoginUser`,
      {
        SoDienThoai: user.username,
        MatKhau: user.password,
        __RequestVerificationToken: csrf,
      },
      {
        redirects: 10,
        tags: { step: "login_post" },
      },
    );
    mLogin.add(Date.now() - t0);

    // Login thành công nếu không bị redirect về trang Login
    const finalUrl = res.url || "";
    const isOnLogin = finalUrl.includes("/Login");
    loggedIn = res.status === 200 && !isOnLogin;

    if (!loggedIn) {
      console.error(
        `VU${__VU} [${user.username}] LOGIN THẤT BẠI` +
          ` | status=${res.status}` +
          ` | url=${finalUrl}` +
          ` | body_snippet=${(res.body || "").substring(0, 200).replace(/\s+/g, " ")}`,
      );
    }

    check(res, { login_success: () => loggedIn });
  });

  if (!loggedIn) {
    rSuccess.add(false);
    return;
  }

  sleep(0.3);

  // ── 2. Load trang thi ─────────────────────────────────────────────────────
  group("2_load_exam", function () {
    const t0 = Date.now();
    const res = http.get(`${BASE_URL}/STest/Index?LHID=${lhid}`, {
      tags: { step: "exam_load" },
    });
    mLoad.add(Date.now() - t0);

    const finalUrl = res.url || "";
    const redirectedOut =
      finalUrl.includes("/Login") || finalUrl.includes("/EClassroom");
    const body = res.body || "";

    if (redirectedOut || res.status !== 200) {
      console.error(
        `VU${__VU} [${user.username}] LOAD ĐỀ THI THẤT BẠI` +
          ` | LHID=${lhid}` +
          ` | status=${res.status}` +
          ` | url=${finalUrl}`,
      );
      check(res, { exam_page_ok: () => false });
      return;
    }

    check(res, {
      exam_page_ok: (r) => r.status === 200 && !redirectedOut,
      exam_has_session: () => body.includes("var examSession ="),
    });

    examSession = parseExamSession(body);

    if (!examSession) {
      // Debug: in 500 ký tự quanh vị trí examSession trong HTML
      const idx = body.indexOf("examSession");
      const snippet =
        idx >= 0
          ? body.substring(Math.max(0, idx - 20), idx + 200)
          : body.substring(0, 400);
      console.warn(
        `VU${__VU} [${user.username}] examSession = null` +
          ` | LHID=${lhid} | snippet: ${snippet.replace(/\s+/g, " ")}`,
      );
      return;
    }

    questionIds = examSession.QuestionOrder || [];

    if (!questionIds.length) {
      questionIds = parseQuestionIds(body);
      if (questionIds.length) {
        console.warn(
          `VU${__VU}: dùng fallback parseQuestionIds, count=${questionIds.length}`,
        );
      }
    }

    // Log thành công lần đầu (chỉ VU 1 để không spam)
    if (__VU === 1 && __ITER === 0) {
      console.log(
        `VU1 exam ok | LHID=${examSession.IDLH}` +
          ` | IDDeThi=${examSession.IDDeThi}` +
          ` | Số câu=${questionIds.length}`,
      );
    }
  });

  if (!examSession || !questionIds.length) {
    rSuccess.add(false);
    return;
  }

  sleep(0.3);

  // ── 3. AutoSave (chọn ~60% số câu) ───────────────────────────────────────
  group("3_autosave", function () {
    const toAnswer = questionIds.slice(0, Math.ceil(questionIds.length * 0.6));

    for (let i = 0; i < toAnswer.length; i++) {
      const t0 = Date.now();
      const res = http.post(
        `${BASE_URL}/STest/AutoSave`,
        JSON.stringify({
          IDLH: examSession.IDLH,
          IDCH: toAnswer[i],
          AnswerId: Math.floor(Math.random() * 4) + 1,
          ChosenAt: Date.now(),
        }),
        {
          headers: { "Content-Type": "application/json" },
          tags: { step: "autosave" },
        },
      );
      mAutosave.add(Date.now() - t0);
      check(res, { autosave_ok: (r) => r.status === 200 });

      sleep(0.2 + Math.random() * 0.3);

      if (i > 0 && i % 8 === 0) {
        http.get(`${BASE_URL}/STest/PingSession?idlh=${lhid}`, {
          tags: { step: "ping" },
        });
      }
    }
  });

  sleep(0.2);

  // ── 4. Nộp bài ────────────────────────────────────────────────────────────
  group("4_submit", function () {
    const answers = questionIds.map((idch) => ({
      IDCH: idch,
      Answer: Math.floor(Math.random() * 4) + 1,
    }));

    const elapsed = Math.max(
      30,
      Math.round((Date.now() - examSession.StartTimestamp) / 1000),
    );

    const t0 = Date.now();
    const res = http.post(
      `${BASE_URL}/STest/SubmitExamAjax`,
      JSON.stringify({
        IDLH: examSession.IDLH,
        IDDeThi: examSession.IDDeThi,
        IDND: examSession.IDND,
        ThoiGianSec: elapsed,
        TGBDLamBaiThi: examSession.StartTimestamp,
        Answers: answers,
      }),
      {
        headers: { "Content-Type": "application/json" },
        tags: { step: "submit" },
      },
    );
    mSubmit.add(Date.now() - t0);
    cSubmit.add(1);

    let success = false;
    try {
      const body = JSON.parse(res.body);
      success = body.success === true;
      if (!success) {
        console.warn(
          `VU${__VU} [${user.username}] nộp thất bại: ${body.message}`,
        );
      }
    } catch (e) {
      console.error(
        `VU${__VU} parse submit response lỗi: ${res.body ? res.body.substring(0, 100) : "empty"}`,
      );
    }

    check(res, {
      submit_200: (r) => r.status === 200,
      submit_success: () => success,
    });
    rSuccess.add(success);
    if (success) cSuccess.add(1);
  });

  sleep(1);
}

// ── Tóm tắt kết quả ───────────────────────────────────────────────────────────
export function handleSummary(data) {
  const m = data.metrics;
  const get = (key, field) =>
    m[key] && m[key].values[field] != null ? m[key].values[field] : null;
  const fmt = (v, unit) =>
    v == null
      ? "N/A"
      : unit === "%"
        ? (v * 100).toFixed(1) + "%"
        : unit === "ms"
          ? Math.round(v) + "ms"
          : String(v);

  const totalAttempt = get("exam_submit_total", "count") ?? 0;
  const totalSuccess = get("exam_submit_success", "count") ?? 0;
  const totalFail = totalAttempt - totalSuccess;
  const successPct =
    totalAttempt > 0
      ? ((totalSuccess / totalAttempt) * 100).toFixed(1) + "%"
      : "N/A";

  const modeLabel =
    MODE === 1
      ? `1 lần/VU | ${VUS} VUs          `
      : `${MAX_SUBMITS} lần tổng | ${VUS} VUs    `;

  const lines = [
    "",
    "╔══════════════════════════════════════════╗",
    "║     KẾT QUẢ LOAD TEST — HỆ THỐNG THI    ║",
    "╠══════════════════════════════════════════╣",
    `║  Mode ${MODE} — ${modeLabel.substring(0, 28)}║`,
    "╠══════════════════════════════════════════╣",
    `║  Tổng lần thi        : ${String(totalAttempt).padEnd(18)}║`,
    `║  ✔ Thành công        : ${String(totalSuccess).padEnd(18)}║`,
    `║  ✘ Thất bại          : ${String(totalFail).padEnd(18)}║`,
    `║  Tỉ lệ thành công    : ${successPct.padEnd(18)}║`,
    `║  HTTP lỗi            : ${fmt(get("http_req_failed", "rate"), "%").padEnd(18)}║`,
    "╠══════════════════════════════════════════╣",
    `║  Login p95           : ${fmt(get("dur_login_ms", "p(95)"), "ms").padEnd(18)}║`,
    `║  Load đề p95         : ${fmt(get("dur_exam_load_ms", "p(95)"), "ms").padEnd(18)}║`,
    `║  AutoSave p95        : ${fmt(get("dur_autosave_ms", "p(95)"), "ms").padEnd(18)}║`,
    `║  Nộp bài p95         : ${fmt(get("dur_submit_ms", "p(95)"), "ms").padEnd(18)}║`,
    `║  HTTP tổng p95       : ${fmt(get("http_req_duration", "p(95)"), "ms").padEnd(18)}║`,
    "╚══════════════════════════════════════════╝",
    "",
  ];

  return {
    stdout: lines.join("\n"),
    "k6-result.json": JSON.stringify(data, null, 2),
  };
}
