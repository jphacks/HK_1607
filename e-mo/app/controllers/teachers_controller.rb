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
  end

  # 送信元のユーザを特定できないとユーザ単位での平均値は出せない
  # ユーザ側の画面を赤くするのも無理や、やるならFaceTracking側？
  # POST /teacher/socket FaceTrackingアプリでのPOST送信先
  def expression_data
    # サーバデバッグ用出力
    p "ログインID: #{params['userId']}"
    p "表情値: #{params['satisfaction']}"
    # 送信元のログインIDの取得
    userId = params["userId"]
    # 送信元の表情の値取得
    satisfaction = params["satisfaction"]

    # user = User.find_by(login_id: userId)
    # 取得した値をDBに保存
    UserExpression.create(user_id: User.find_by(login_id: userId).id, expression: satisfaction.to_i)

    # 表情値が40未満の場合
    if satisfaction.to_i < 40
      StudentWarningJob.perform_later(userId, "warning")
    # 表情値が40以上の場合
    else
      StudentWarningJob.perform_later(userId, "info")
    end

    # 初期値として(エラー等で算出が出来なかった時用に)50を代入
    expression = 50
    # 各生徒の表情値を格納するための配列を用意
    expressions = []

    # 全生徒に対し処理
    User.where(teacher_flag: false).each do |student|
      # デバック用
      # <User id: 1, login_id: "s9999", student_name: "久保田義隆", password: "4545", connected_flag: true, teacher_flag: false, admin_flag: false, created_at: "2016-11-03 07:27:41", updated_at: "2016-11-05 08:55:47">
      # <User id: 2, login_id: "s1234", student_name: "ショーンオチンコ", password: "4545", connected_flag: false, teacher_flag: false, admin_flag: false, created_at: "2016-11-03 07:28:29", updated_at: "2016-11-04 04:26:21">
      p student
      p "はいはいはい"

      # 当該生徒の最新の表情値のデータを取得
      user_expression = UserExpression.where(user_id: student.id).last

      # デバック用
      # <UserExpression id: 5, expression: 39, face_img: nil, user_id: 1, created_at: "2016-11-05 09:10:54", updated_at: "2016-11-05 09:10:54">
      # nil
      p user_expression

      # 当該生徒の表情値のデータが存在する場合
      if user_expression
        # 表情値を取得
        expression = user_expression.expression
        # 直近10分以内に取得したデータだった場合は現在の講義でのデータと判断し、平均値の算出基に加える
        expressions.push(expression) if user_expression.updated_at >= Time.now - 60
      end
    end

    # デバッグ用
    puts
    p expressions

    # 現在授業に参加している生徒の表情値の平均値を算出
    sum = 0
    expressions.each do |num|
      sum += num
    end
    expression = sum / expressions.size
    ExpressionAverage.create(expression_avg: expression)
    # 先生への送信用にJSONオブジェクトを生成
    expression_json = { expression: expression }.to_json

    # TODO コメント
    expression_averages = ExpressionAverage.order("id").limit(20)
    p expression_averages
    chart_data = {}
    i = 0
    expression_averages.each do |avg|
      chart_data.store(i, avg.expression_avg)
      i += 1
    end

    # 理解度の値を先生宛てで送信
    SendExpressionDataJob.perform_later(expression_json, chart_data.to_json)

    # 画面の生成、遷移が行われないためレスポンスコードのみ返す
    head 200

    # render "index"
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
