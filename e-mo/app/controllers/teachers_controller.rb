class TeachersController < ApplicationController
  # アクセスフィルターを本コントローラのアクション前処理に設定
  before_action :access_filter
  # FaceTrackingアプリからの表情の値取得時にはフィルターを無視
  skip_before_action :access_filter, only: [:expression_data]
  # FaceTrackingアプリからの表情の値取得時には認証情報を無視
  skip_before_action :authenticate_user!, only: [:expression_data]
  # FaceTrackingアプリからの表情の値取得時にはCSRF対策を無効
  protect_from_forgery except: [:expression_data]

  # GET /teacher
  def index
    @chart_data = []
    10.times do |i|
      @chart_data.push()
    end
  end

  # 送信元のユーザを特定できないとユーザ単位での平均値は出せない
  # ユーザ側の画面を赤くするのも無理や、やるならFaceTracking側？
  # POST /teacher/socket FaceTrackingアプリでのPOST送信先
  def expression_data
    # サーバデバッグ用出力
    p "ログインID: #{params['userId']}"
    p "スマイルデータ: #{params['smileData']}"
    # TODO 表情の値から理解度の算出
    userId = params["userId"]
    smileData = params["smileData"]
    if smileData.to_i < 40
      StudentWarningJob.perform_later(userId, "warning")
    else
      StudentWarningJob.perform_later(userId, "info")
    end
    # クライアントへの送信用にJSONオブジェクトを生成
    expression_json = {smile: smileData}.to_json
    # 理解度の値を先生宛てで送信
    SendExpressionDataJob.perform_later(expression_json)
    # 画面の生成、遷移が行われないためレスポンスコードのみ返す
    head 200
  end

  private
    # カレントユーザでの先生画面へのアクセスフィルター
    def access_filter
      # 先生でログインしている場合
      if current_user.teacher_flag
        # ...
      # 生徒でログインしている場合
      else
        # 生徒画面へリダイレクト
        redirect_to student_url
      end
    end
end
