import { pipeline, env } from '@xenova/transformers';

env.allowLocalModels = false;
env.localModelPath = '';
env.useBrowserCache = true;

env.remoteHost = 'https://huggingface.co';

env.backends.onnx.wasm.numThreads = 1;
env.backends.onnx.wasm.simd = true;


let classifier = null;
let loadingPromise = null;

export async function loadModel() {
  if (classifier) return classifier;


  if (!loadingPromise) {
    loadingPromise = pipeline(
      'sentiment-analysis',
      'Xenova/distilbert-base-uncased-finetuned-sst-2-english',
      {
        progress_callback: (p) => console.log("Model loading:", p),
      }
    );
  }


  classifier = await loadingPromise;
  return classifier;
}

const STOCK_MAP = {
  AAPL: ["apple", "iphone", "mac", "ipad"],
  MSFT: ["microsoft", "windows", "azure", "xbox"],
  GOOGL: ["google", "alphabet", "youtube", "android"],
  AMZN: ["amazon", "aws", "prime"],
  META: ["meta", "facebook", "instagram", "whatsapp"],
  NVDA: ["nvidia", "gpu", "ai chip"],
  TSLA: ["tesla", "elon musk", "ev"],
  NFLX: ["netflix", "streaming"],
  ORCL: ["oracle"],
  IBM: ["ibm"],
  JPM: ["jpmorgan", "jp morgan", "chase bank"],
  BAC: ["bank of america"],
  WFC: ["wells fargo"],
  GS: ["goldman sachs"],
  MS: ["morgan stanley"],
  C: ["citigroup"],
  WMT: ["walmart"],
  TGT: ["target"],
  COST: ["costco"],
  HD: ["home depot"],
  NKE: ["nike"],
  SBUX: ["starbucks"],
  MCD: ["mcdonald", "mcdonalds"],
  F: ["ford"],
  GM: ["general motors", "gm"],
  BA: ["boeing"],
  CAT: ["caterpillar"],
  GE: ["general electric"],
  JNJ: ["johnson & johnson", "jnj"],
  PFE: ["pfizer"],
  MRK: ["merck"],
  ABBV: ["abbvie"],
  UNH: ["unitedhealth"],
  XOM: ["exxon", "exxonmobil"],
  CVX: ["chevron"],
  BP: ["bp"],
  SHEL: ["shell"],
  AMD: ["amd", "advanced micro devices"],
  INTC: ["intel"],
  TSM: ["tsmc", "taiwan semiconductor"],
  QCOM: ["qualcomm"],
  DIS: ["disney"],
  CMCSA: ["comcast"],
  VZ: ["verizon"],
  T: ["at&t", "att"],
  COIN: ["coinbase"],
  MSTR: ["microstrategy"],
  SPY: ["s&p 500", "sp500"],
  QQQ: ["nasdaq"],
};


// Detect stock from title
function detectStock(title) {
  if (!title) return "Unknown";


  const upper = title.toUpperCase();


  const tickerMatch = upper.match(/\b[A-Z]{2,5}\b/);
  if (tickerMatch && STOCK_MAP[tickerMatch[0]]) {
    return tickerMatch[0];
  }


  const lower = title.toLowerCase();


  for (const [ticker, keywords] of Object.entries(STOCK_MAP)) {
    if (keywords.some((k) => lower.includes(k))) {
      return ticker;
    }
  }


  return "MARKET";
}


// Reduce overconfidence
function calibrateScore(score) {


  // squash extreme confidence (logistic-style compression)
  const adjusted = 1 / (1 + Math.exp(-4 * (score - 0.5)));


  // convert to %
  return Math.round(adjusted * 100);
}


function hasWeakLanguage(title) {
  const weakWords = [
    "may", "might", "could", "rumor", "possible",
    "uncertain", "reportedly", "expected"
  ];


  const lower = title.toLowerCase();
  return weakWords.some(w => lower.includes(w));
}


export async function analyzeHeadline(title) {
  try {
    if (!title || typeof title !== "string") {
      return {
        sentiment: "neutral",
        confidence: 0,
        impact: "MARKET",
      };
    }


    const model = await loadModel();


    const cleanTitle = title.trim().slice(0, 256);


    const result = await model(cleanTitle);


    let label = result?.[0]?.label || "";
    let score = result?.[0]?.score || 0;


    let confidence = calibrateScore(score);


    if (hasWeakLanguage(cleanTitle)) {
      confidence = Math.max(40, confidence - 25);
    }

    let sentiment = "neutral";
    if (confidence > 60) {
      if (label.toLowerCase().includes("positive")) sentiment = "bullish";
      if (label.toLowerCase().includes("negative")) sentiment = "bearish";
    }

    const stock = detectStock(cleanTitle);

    return {
      sentiment,
      confidence,
      impact: stock,
    };

  } catch (err) {
    console.error("MODEL FAILURE:", err);

    return {
      sentiment: "neutral",
      confidence: 0,
      impact: "MARKET",
    };
  }
}

